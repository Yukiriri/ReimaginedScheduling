﻿using ReimaginedScheduling.Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using Vanara.Security.AccessControl;

namespace ReimaginedScheduling.Services
{
    public class GameRules
    {
        public GameRules()
        {
            User32.GetWindowRect(User32.GetDesktopWindow(), out _deskRect);
        }

        public bool IsGameProcess(HWND hwnd)
        {
            User32.GetClientRect(hwnd, out var wndRect);
            if (wndRect.Size != _deskRect.Size)
            {
                var ci = new User32.CURSORINFO();
                if (User32.GetCursorInfo(ref ci) && ci.flags != User32.CursorState.CURSOR_HIDDEN)
                    return false;
            }
            if (User32.GetWindowThreadProcessId(hwnd, out var pid) != 0)
            {
                var gpuUsage = _pm.GetGPUUsage(pid);
                var gpuMemMB = _pm.GetGPUMemUsage(pid) >> 20;
                if (gpuUsage >= Config.GPUUsageThreshold && gpuMemMB >= Config.GPUMemUsageThreshold)
                    return true;
            }
            return false;
        }

        public void AddGameProcess(HWND hwnd)
        {
            if (_processDataList.ContainsKey(hwnd))
                return;
            var windowName = new StringBuilder(User32.GetWindowTextLength(hwnd) + 1);
            if (User32.GetWindowText(hwnd, windowName, windowName.Capacity) == 0)
            {
                //Console.WriteLine($"跳过无名窗口：PID={pid}");
                return;
            }
            if (User32.GetWindowThreadProcessId(hwnd, out var pid) == 0)
            {
                Console.WriteLine($"GetWindowThreadProcessId失败：PID={pid}");
                return;
            }
            var hProcess = Kernel32.OpenProcess((uint)(
                Kernel32.ProcessAccess.PROCESS_SET_INFORMATION |
                Kernel32.ProcessAccess.PROCESS_SET_LIMITED_INFORMATION | 
                Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION | 
                Kernel32.ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION), false, pid);
            if (hProcess.IsNull)
            {
                Console.WriteLine($"OpenProcess失败：PID={pid} Error={Win32Error.GetLastError()}");
                return;
            }
            var imgFileName = new StringBuilder(Kernel32.MAX_PATH);
            if (Kernel32.GetProcessImageFileName(hProcess, imgFileName, (uint)imgFileName.Capacity) == 0)
            {
                Console.WriteLine($"GetProcessImageFileName失败：PID={pid} Error={Win32Error.GetLastError()}");
                return;
            }
            var exeName = new Regex("""(?!.*\\).+(?=\.exe)""").Match(imgFileName.ToString()).Value;
            if (!Kernel32.SetPriorityClass(hProcess, Kernel32.CREATE_PROCESS.HIGH_PRIORITY_CLASS))
            {
                Console.WriteLine($"SetPriorityClass失败：PID={pid} Error={Win32Error.GetLastError()}");
            }
            _processDataList[hwnd] = new ProcessData(hProcess, pid, exeName, windowName.ToString());
            Console.WriteLine($"列入游戏进程：PID=[{pid}] exe=[{exeName}] name=[{windowName}]");
            Console.WriteLine(Config.ConsoleSplitRow);
        }

        public void UpdateSampling()
        {
            foreach (var processData in _processDataList)
            {
                var hwnd = processData.Key;
                var pData = processData.Value;
                if (User32.IsWindow(hwnd))
                {
                    var thUsage = _pm.GetProcessThreadUsage(pData.exeName, Config.MaxThreadMonitorCount);
                    var thID = _pm.GetProcessThreadID(pData.exeName, Config.MaxThreadMonitorCount);
                    if (pData.ThreadUsage.Count == 0)
                    {
                        for (int i = 0; i < thUsage.Count; i++)
                            pData.ThreadUsage[i] = 0;
                    }
                    for (int i = 0; i < thUsage.Count; i++)
                    {
                        pData.ThreadUsage[i] += thUsage[i] / Config.ThreadSamplingCount;
                    }
                    pData.ThreadID = thID;
                    if (++pData.SamplingCount == Config.ThreadSamplingCount)
                    {
                        ReimagineScheduling(ref pData);
                        pData.SamplingCount = 0;
                        pData.ThreadUsage.Clear();
                    }
                    _processDataList[hwnd] = pData;
                }
                else
                {
                    _pm.ClearProcessThreadMonitor(pData.exeName, Config.MaxThreadMonitorCount);
                    _processDataList.Remove(hwnd);
                    Console.WriteLine($"移出失效进程：PID=[{pData.PID}] exe=[{pData.exeName}] name=[{pData.WindowName}]");
                    Console.WriteLine(Config.ConsoleSplitRow);
                }
            }
        }

        private void ReimagineScheduling(ref ProcessData processData)
        {
            if (Config.MaxExclusiveCount <= 0)
                return;

            var highLoadTh = from th in processData.ThreadUsage
                             where th.Value >= 0
                             orderby th.Value descending
                             select th;
            var usedExclusiveCount = Math.Min(highLoadTh.Count(), Config.MaxExclusiveCount);
            var peCores = CPUSetInfo.PhysicalPECoreList;
            SortedDictionary<uint, int[]> exclusiveCores = []; //key = cpuid, value = [tid, usage]
            List<uint> sharedCores = [];
            for (int i = 0; i < peCores.Count; i++)
            {
                var cpuid = peCores[i];
                if (i < usedExclusiveCount)
                {
                    var th = highLoadTh.ElementAt(i);
                    var thInstanceID = th.Key;
                    var thUsage = (int)th.Value;
                    var tid = (int)processData.ThreadID[thInstanceID];
                    exclusiveCores[cpuid] = [tid, thUsage];
                }
                else
                {
                    sharedCores.Add(cpuid);
                }
            }

            static bool SetThreadCPUID(int tid, uint? cpuid, [Optional] int usage)
            {
                using var hThread = Kernel32.OpenThread((uint)(
                    Kernel32.ThreadAccess.THREAD_SET_INFORMATION |
                    Kernel32.ThreadAccess.THREAD_SET_LIMITED_INFORMATION |
                    Kernel32.ThreadAccess.THREAD_QUERY_INFORMATION |
                    Kernel32.ThreadAccess.THREAD_QUERY_LIMITED_INFORMATION), false, (uint)tid);
                if (hThread.IsNull)
                {
                    Console.WriteLine($"OpenThread失败：TID={tid} Error={Win32Error.GetLastError()}");
                    return false;
                }
                uint[] id = cpuid == null ? [] : [cpuid.Value];
                if (!Kernel32.SetThreadSelectedCpuSets(hThread, id, (uint)id.Length))
                {
                    Console.WriteLine($"SetThreadSelectedCpuSets失败：TID={tid}");
                    return false;
                }
                if (id.Length > 0)
                {
                    //Kernel32.GetThreadGroupAffinity(hThread, out var ga);
                    //Kernel32.GetThreadIdealProcessorEx(hThread, out var ip);
                    Console.WriteLine($"{cpuid}={tid,-5}({usage}%)");
                }
                else
                {
                    Console.WriteLine($"{tid}=0");
                }
                return true;
            }

            Console.WriteLine($"更新：PID={processData.PID} name={processData.WindowName}");
            //Console.WriteLine($"独占{exclusiveCores.Count} / 共享{sharedCores.Count}");
            var minEC = Math.Min(processData.LastExclusiveCores.Count, exclusiveCores.Count);
            var maxEC = Math.Max(processData.LastExclusiveCores.Count, exclusiveCores.Count);
            for (int i = 0; i < maxEC; i++)
            {
                KeyValuePair<uint, int[]>? oldEC = null;
                var oldTID = 0;
                var oldUsage = 0;
                KeyValuePair<uint, int[]>? newEC = null;
                uint newCPUID = 0;
                var newTID = 0;
                var newUsage = 0;

                if (processData.LastExclusiveCores.Count > minEC)
                {
                    oldEC = processData.LastExclusiveCores.ElementAt(i);
                    oldTID = oldEC.Value.Value[0];
                    oldUsage = oldEC.Value.Value[1];
                }
                if (exclusiveCores.Count > minEC)
                {
                    newEC = exclusiveCores.ElementAt(i);
                    newCPUID = newEC.Value.Key;
                    newTID = newEC.Value.Value[0];
                    newUsage = newEC.Value.Value[1];
                }
                if (oldEC.HasValue)
                {
                    if (newEC.HasValue)
                    {
                        if (newTID == oldTID)
                            continue;
                        if (Math.Abs(newUsage - oldUsage) <= Config.ThreadAntiJitterUsageThreshold)
                            continue;
                    }
                    SetThreadCPUID(oldTID, null);
                }
                if (newEC.HasValue)
                {
                    SetThreadCPUID(newTID, newCPUID, newUsage);
                }
            }
            processData.LastExclusiveCores = exclusiveCores;
            if (!Kernel32.SetProcessDefaultCpuSets(processData.hProcess, [.. sharedCores], (uint)sharedCores.Count))
            {
                Console.WriteLine($"SetProcessDefaultCpuSets失败，独占有可能被污染");
            }
            Console.WriteLine(Config.ConsoleSplitRow);
        }

        public static bool SeDebug()
        {
            if (!AdvApi32.OpenProcessToken(Kernel32.GetCurrentProcess(), AdvApi32.TokenAccess.TOKEN_ADJUST_PRIVILEGES | AdvApi32.TokenAccess.TOKEN_QUERY, out var hToken))
                return false;
            hToken.AdjustPrivilege(SystemPrivilege.Debug, AdvApi32.PrivilegeAttributes.SE_PRIVILEGE_ENABLED);
            return hToken.HasPrivilege(SystemPrivilege.Debug);
        }

        private readonly PerformanceMonitor _pm = new();
        private readonly Dictionary<HWND, ProcessData> _processDataList = [];
        private RECT _deskRect = new(0, 0, 0, 0);
    }

    internal struct ProcessData(Kernel32.SafeHPROCESS hProcess, uint PID, string exeName, string windowName)
    {
        public Kernel32.SafeHPROCESS hProcess = hProcess;
        public uint PID = PID;
        public string exeName = exeName;
        public string WindowName = windowName;

        public Dictionary<int, double> ThreadUsage = [];
        public Dictionary<int, double> ThreadID = [];
        public int SamplingCount = 0;

        public SortedDictionary<uint, int[]> LastExclusiveCores = [];
    }
}
