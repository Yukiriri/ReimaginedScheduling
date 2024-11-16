using ReimaginedScheduling.Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
            Console.Write($"{"Name",-30}");
            foreach (var cpuid in CPUSetInfo.PhysicalPCoreList.Take(Config.MaxExclusiveCount)) Console.Write($"{$"CPU{cpuid - CPUSetInfo.BeginCPUID}",-13}");
            Console.WriteLine();
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
            if (_processDataList.Where(pd => pd.hWnd == hwnd).Any())
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
            _processDataList.Add(new ProcessData(hwnd, hProcess, pid, exeName, windowName.ToString()));
            Console.WriteLine($"+[{windowName}]");
        }

        public void UpdateSampling()
        {
            _pm.Update();
            for (int pdi = _processDataList.Count - 1; pdi >= 0; pdi--)
            {
                var pd = _processDataList[pdi];
                if (!User32.IsWindow(pd.hWnd))
                {
                    _processDataList.RemoveAt(pdi);
                    _pm.ClearProcessThreadMonitor(pd.exeName, Config.MaxThreadMonitorCount);
                    Console.WriteLine($"-[{pd.WindowName}]");
                }
            }
            for (int pdi = _processDataList.Count - 1; pdi >= 0; pdi--)
            {
                var pd = _processDataList[pdi];
                var thUsage = _pm.GetProcessThreadIDWithUsage(pd.exeName, pd.ThreadUsage.Length);

                for (int i = 0; i < pd.ThreadUsage.Length; i++)
                {
                    pd.ThreadUsage[i].threadID = thUsage[i].threadID;
                    pd.ThreadUsage[i].usage += (uint)(thUsage[i].usage / Config.ThreadSamplingCount);
                }
                if (++pd.SamplingCount == Config.ThreadSamplingCount)
                {
                    ReimagineScheduling(ref pd);
                    pd.SamplingCount = 0;
                    for (int i = 0; i < pd.ThreadUsage.Length; i++)
                        pd.ThreadUsage[i] = new();
                }
                _processDataList[pdi] = pd;
            }
        }

        static void ReimagineScheduling(ref ProcessData processData)
        {
            if (Config.MaxExclusiveCount <= 0)
                return;

            var highLoadTh = processData.ThreadUsage
                .Where(pdtu => pdtu.usage >= Config.ThreadUsageThreshold)
                .OrderByDescending(pdtu => pdtu.usage)
                .Where((pdtu, index) => index < Config.MaxExclusiveCount)
                .ToArray();
            var peCores = CPUSetInfo.PhysicalPECoreList;
            var exclusiveCores = peCores
                .Where((cpuid, index) => index < highLoadTh.Length)
                .Select((cpuid, index) => new ExclusiveCore(cpuid, highLoadTh[index].threadID, highLoadTh[index].usage))
                .ToArray();
            var sharedCores = peCores
                .Where((cpuid, index) => index >= highLoadTh.Length && index < peCores.Count)
                .ToArray();

            static bool SetThreadCPUID(uint tid, uint? cpuid)
            {
                using var hThread = Kernel32.OpenThread((uint)(
                    Kernel32.ThreadAccess.THREAD_SET_INFORMATION |
                    Kernel32.ThreadAccess.THREAD_SET_LIMITED_INFORMATION |
                    Kernel32.ThreadAccess.THREAD_QUERY_INFORMATION |
                    Kernel32.ThreadAccess.THREAD_QUERY_LIMITED_INFORMATION), false, tid);
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
                return true;
            }

            var beDefaultTIDList = processData.LastExclusiveCores
                .Select((lec, index) => index < exclusiveCores.Length ? (exclusiveCores.Where(ec => lec.TID == ec.TID).Any() ? 0 : lec.TID) : lec.TID)
                .ToArray();
            if (beDefaultTIDList.Where(tid => tid != 0).Any())
            {
                Console.Write($"{$"[{processData.WindowName}]",-30}");
                for (int i = 0; i < Config.MaxExclusiveCount; i++) Console.Write($"{"|",-13}");
                Console.WriteLine();

                Console.Write($"{$"[{processData.WindowName}]({exclusiveCores.Length}/{sharedCores.Length})",-30}");
                for (int i = 0; i < Config.MaxExclusiveCount; i++)
                {
                    var tid = beDefaultTIDList[i];
                    if (tid != 0)
                    {
                        SetThreadCPUID(tid, null);
                        processData.LastExclusiveCores[i] = new();
                        Console.Write($"{"X",-13}");
                    }
                    else
                    {
                        Console.Write($"{"|",-13}");
                    }
                }
                Console.WriteLine();
            }

            var beOverrideTIDList = processData.LastExclusiveCores
                .Select((lec, index) => index < exclusiveCores.Length && lec.TID != exclusiveCores[index].TID ? exclusiveCores[index] : new())
                .ToArray();
            //var lecUsage = processData.LastExclusiveCores
            //    .Where((lec, index) => beOverrideTIDList[index].Usage > 0)
            //    .Aggregate(0u, (sum, next) => sum + next.Usage) / beOverrideTIDList.Length;
            //var ecUsage = beOverrideTIDList
            //    .Aggregate(0u, (sum, next) => sum + next.Usage) / beOverrideTIDList.Length;
            if (beOverrideTIDList.Where(tid => tid.TID != 0).Any() /*&& Math.Abs(lecUsage - ecUsage) >= Config.ThreadUsageOffsetThreshold*/)
            {
                Console.Write($"{$"[{processData.WindowName}]",-30}");
                for (int i = 0; i < Config.MaxExclusiveCount; i++) Console.Write($"{"|",-13}");
                Console.WriteLine();

                Console.Write($"{$"[{processData.WindowName}]({exclusiveCores.Length}/{sharedCores.Length})",-30}");
                for (int i = 0; i < Config.MaxExclusiveCount; i++)
                {
                    var otid = beOverrideTIDList[i];
                    var cpuid = otid.CPUID;
                    var tid = otid.TID;
                    var usage = otid.Usage;
                    if (tid != 0)
                    {
                        SetThreadCPUID(tid, cpuid);
                        processData.LastExclusiveCores[i] = otid;
                        Console.Write($"{$"{tid,-5}({usage}%)",-13}");
                    }
                    else
                    {
                        Console.Write($"{"|",-13}");
                    }
                }
                Console.WriteLine();
            }
            if (!Kernel32.SetProcessDefaultCpuSets(processData.hProcess, sharedCores, (uint)sharedCores.Length))
            {
                Console.WriteLine($"[{processData.WindowName}] SetProcessDefaultCpuSets失败，独占有可能被污染");
            }
        }

        public static bool SeDebug()
        {
            if (!AdvApi32.OpenProcessToken(Kernel32.GetCurrentProcess(), AdvApi32.TokenAccess.TOKEN_ADJUST_PRIVILEGES | AdvApi32.TokenAccess.TOKEN_QUERY, out var hToken))
                return false;
            hToken.AdjustPrivilege(SystemPrivilege.Debug, AdvApi32.PrivilegeAttributes.SE_PRIVILEGE_ENABLED);
            return hToken.HasPrivilege(SystemPrivilege.Debug);
        }

        private readonly PerformanceMonitor _pm = new();
        private readonly List<ProcessData> _processDataList = [];
        private RECT _deskRect = new(0, 0, 0, 0);
    }

    struct ProcessData(HWND hWnd, Kernel32.SafeHPROCESS hProcess, uint PID, string exeName, string windowName)
    {
        public HWND hWnd = hWnd;
        public string WindowName = windowName;
        public Kernel32.SafeHPROCESS hProcess = hProcess;
        public uint PID = PID;
        public string exeName = exeName;

        public (uint threadID, uint usage)[] ThreadUsage = new (uint, uint)[Config.MaxThreadMonitorCount];
        public int SamplingCount = 0;

        public ExclusiveCore[] LastExclusiveCores = new ExclusiveCore[Config.MaxExclusiveCount];
    }

    struct ExclusiveCore(uint CPUID, uint TID, uint Usage)
    {
        public uint CPUID = CPUID;
        public uint TID = TID;
        public uint Usage = Usage;
    }
}
