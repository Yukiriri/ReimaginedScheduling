using ReimaginedScheduling.Services.Utils;
using System;
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
                var gpuUsage = _performanceMonitor.GetGPUUsage(pid);
                var gpuMemMB = _performanceMonitor.GetGPUMemUsage(pid) >> 20;
                if (gpuUsage >= Config.GPUUsageThreshold && gpuMemMB >= Config.GPUMemUsageThreshold)
                    return true;
            }
            return false;
        }

        public void AttachGameProcess(HWND hwnd)
        {
            if (hwnd == _processData.hWnd)
                return;
            if (!_processData.hWnd.IsNull)
                DettachGameProcess(true);

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
            _processData = new ProcessData(hwnd, hProcess, pid, exeName, windowName.ToString());
            Console.WriteLine($"+[{windowName}]");
            _processData.ThreadManager.WriteLineCPUID();
        }

        private void DettachGameProcess(bool isRestoreProcess)
        {
            Console.WriteLine($"-[{_processData.WindowName}]");
            Console.WriteLine(Config.ConsoleSplitRow);
            _performanceMonitor.ClearProcessThreadMonitor(_processData.exeName, _processData.ThreadUsage.Length);
            if (isRestoreProcess)
            {
                Kernel32.SetPriorityClass(_processData.hProcess, Kernel32.CREATE_PROCESS.NORMAL_PRIORITY_CLASS);
                Kernel32.SetProcessDefaultCpuSets(_processData.hProcess, null, 0);
                foreach (var ca in _processData.ThreadManager.CurrentAttribution)
                {
                    if (!ca.IsNull)
                        SetThreadCPUID((uint)ca.TID, null);
                }
            }
            _processData.hWnd = HWND.NULL;
        }

        public void UpdateSampling()
        {
            _performanceMonitor.Update();
            if (_processData.hWnd.IsNull)
                return;
            if (!User32.IsWindow(_processData.hWnd))
            {
                DettachGameProcess(false);
                return;
            }

            var thUsage = _performanceMonitor.GetProcessThreadIDWithUsage(_processData.exeName, _processData.ThreadUsage.Length);
            for (int i = 0; i < _processData.ThreadUsage.Length; i++)
            {
                _processData.ThreadUsage[i].ThreadID = thUsage[i].ThreadID;
                _processData.ThreadUsage[i].Usage += thUsage[i].Usage / Config.ThreadSamplingCount;
            }
            if (++_processData.SamplingCount == Config.ThreadSamplingCount)
            {
                ReimagineScheduling();
                _processData.SamplingCount = 0;
                for (int i = 0; i < _processData.ThreadUsage.Length; i++)
                    _processData.ThreadUsage[i] = new();
            }
        }

        private void ReimagineScheduling()
        {
            ref var manager = ref _processData.ThreadManager;
            if (manager.AvailablePCoreCount <= 0)
                return;

            manager.Append(_processData.ThreadUsage);
            if (manager.IsNeedUpdate)
            {
                manager.WriteLineThreadMap();
                if (manager.IsNeedRestore)
                {
                    foreach (var crt in manager.CurrentRestoreThreads)
                    {
                        if (!crt.IsNull)
                            SetThreadCPUID((uint)crt.TID, null);
                    }
                }
                if (manager.IsNeedOverride)
                {
                    foreach (var cot in manager.CurrentOverrideThreads)
                    {
                        if (!cot.IsNull)
                            SetThreadCPUID((uint)cot.TID, cot.CPUID);
                    }
                }
            }
            if (!Kernel32.SetProcessDefaultCpuSets(_processData.hProcess, manager.CurrentSharedCores, (uint)manager.CurrentSharedCores.Length))
            {
                Console.WriteLine($"[{_processData.WindowName}] SetProcessDefaultCpuSets失败，独占有可能被污染");
            }
        }

        static bool SetThreadCPUID(uint tid, uint? cpuid)
        {
            if (tid == 0)
                return false;
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

        public static bool SeDebug()
        {
            if (!AdvApi32.OpenProcessToken(Kernel32.GetCurrentProcess(), AdvApi32.TokenAccess.TOKEN_ADJUST_PRIVILEGES | AdvApi32.TokenAccess.TOKEN_QUERY, out var hToken))
                return false;
            hToken.AdjustPrivilege(SystemPrivilege.Debug, AdvApi32.PrivilegeAttributes.SE_PRIVILEGE_ENABLED);
            return hToken.HasPrivilege(SystemPrivilege.Debug);
        }

        struct ProcessData(HWND hWnd, Kernel32.SafeHPROCESS hProcess, uint PID, string exeName, string windowName)
        {
            public HWND hWnd = hWnd;
            public string WindowName = windowName;
            public Kernel32.SafeHPROCESS hProcess = hProcess;
            public uint PID = PID;
            public string exeName = exeName;

            public PerformanceMonitor.ProcessThreadIDWithUsage[] ThreadUsage = new PerformanceMonitor.ProcessThreadIDWithUsage[Config.ThreadMonitorCount];
            public int SamplingCount = 0;

            public GameThreadManager ThreadManager = new();
        }

        private readonly PerformanceMonitor _performanceMonitor = new();
        private ProcessData _processData = new();
        private RECT _deskRect = new(0, 0, 0, 0);
    }
}
