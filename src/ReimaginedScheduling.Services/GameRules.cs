using ReimaginedScheduling.Services.Utils;
using System;
using System.Text;
using System.Text.RegularExpressions;
using Vanara.PInvoke;

namespace ReimaginedScheduling.Services;

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
            //MyLogger.Debug($"跳过无名窗口：PID={pid}");
            return;
        }
        if (User32.GetWindowThreadProcessId(hwnd, out var pid) == 0)
        {
            MyLogger.Debug($"GetWindowThreadProcessId失败：PID={pid}");
            return;
        }
        var hProcess = Kernel32.OpenProcess((uint)(
            Kernel32.ProcessAccess.PROCESS_SET_INFORMATION |
            Kernel32.ProcessAccess.PROCESS_SET_LIMITED_INFORMATION | 
            Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION | 
            Kernel32.ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION), false, pid);
        if (hProcess.IsNull)
        {
            MyLogger.Debug($"OpenProcess失败：PID={pid} Error={Win32Error.GetLastError()}");
            return;
        }
        var imgFileName = new StringBuilder(Kernel32.MAX_PATH);
        if (Kernel32.GetProcessImageFileName(hProcess, imgFileName, (uint)imgFileName.Capacity) == 0)
        {
            MyLogger.Debug($"GetProcessImageFileName失败：PID={pid} Error={Win32Error.GetLastError()}");
            return;
        }
        var exeName = new Regex(@"(?!.*\\).+(?=\.exe)").Match(imgFileName.ToString()).Value;
        if (!Kernel32.SetPriorityClass(hProcess, Kernel32.CREATE_PROCESS.HIGH_PRIORITY_CLASS))
        {
            MyLogger.Debug($"SetPriorityClass失败：PID={pid} Error={Win32Error.GetLastError()}");
        }
        _processData = new ProcessData(hwnd, hProcess, pid, exeName, windowName.ToString());
    }

    private void DettachGameProcess(bool isRestoreProcess)
    {
        _performanceMonitor.ClearProcessThreadMonitor(_processData.exeName, Config.ThreadMonitorCount);
        if (isRestoreProcess)
        {
            Kernel32.SetPriorityClass(_processData.hProcess, Kernel32.CREATE_PROCESS.NORMAL_PRIORITY_CLASS);
            Kernel32.SetProcessDefaultCpuSets(_processData.hProcess, null, 0);
            foreach (var ca in _processData.ThreadManager.CurrentAttribution)
            {
                if (ca.IsNotNull)
                    SetThreadCPUID(ca.TID, null);
            }
        }
        _processData.hWnd = HWND.NULL;
    }

    public void UpdateSampling()
    {
        ClearConsole();
        _performanceMonitor.Update();
        if (_processData.hWnd.IsNull)
        {
            Console.Write("\r等待前台游戏");
            return;
        }
        if (!User32.IsWindow(_processData.hWnd))
        {
            DettachGameProcess(false);
            return;
        }

        ref var manager = ref _processData.ThreadManager;
        // if (manager.AvailablePCoreCount <= 0)
        //     return;
        var thUsage = _performanceMonitor.GetProcessThreadIDWithUsage(_processData.exeName, Config.ThreadMonitorCount);
        if (manager.Update(thUsage))
        {
            Console.WriteLine($"{_processData.WindowName} PID={_processData.PID} exe={_processData.exeName}.exe");
            Console.WriteLine(manager.ToString());
            foreach (var ca in manager.CurrentAttribution)
            {
                if (ca.IsNotNull)
                    SetThreadCPUID(ca.TID, ca.CPUID);
            }
            if (!Kernel32.SetProcessDefaultCpuSets(_processData.hProcess, manager.CurrentSharedCores, (uint)manager.CurrentSharedCores.Length))
            {
                MyLogger.Info($"[{_processData.WindowName}] SetProcessDefaultCpuSets失败，独占有可能被污染");
            }
        }
    }

    public static bool SeDebug()
    {
        if (!AdvApi32.OpenProcessToken(Kernel32.GetCurrentProcess(), AdvApi32.TokenAccess.TOKEN_ADJUST_PRIVILEGES | AdvApi32.TokenAccess.TOKEN_QUERY, out var hToken))
            return false;
        if (!AdvApi32.LookupPrivilegeValue(null, "SeDebugPrivilege", out var luid))
            return false;
        return AdvApi32.AdjustTokenPrivileges(hToken, false, new(luid, AdvApi32.PrivilegeAttributes.SE_PRIVILEGE_ENABLED), out _).Succeeded;
        // hToken.AdjustPrivilege(SystemPrivilege.Debug, AdvApi32.PrivilegeAttributes.SE_PRIVILEGE_ENABLED);
        // return hToken.HasPrivilege(SystemPrivilege.Debug);
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
            MyLogger.Debug($"OpenThread失败：TID={tid} Error={Win32Error.GetLastError()}");
            return false;
        }
        uint[] id = cpuid == null ? [] : [cpuid.Value];
        if (!Kernel32.SetThreadSelectedCpuSets(hThread, id, (uint)id.Length))
        {
            MyLogger.Debug($"SetThreadSelectedCpuSets失败：TID={tid}");
            return false;
        }
        return true;
    }

    static void ClearConsole()
    {
        #if RELEASE
        Console.SetCursorPosition(0, 0);
        Console.WriteLine(new string(' ', Console.WindowWidth * Console.WindowHeight));
        Console.SetCursorPosition(0, 0);
        #endif
    }

    struct ProcessData(HWND hWnd, Kernel32.SafeHPROCESS hProcess, uint PID, string exeName, string windowName)
    {
        public HWND hWnd = hWnd;
        public string WindowName = windowName;
        public Kernel32.SafeHPROCESS hProcess = hProcess;
        public string exeName = exeName;
        public uint PID = PID;
        public GameThreadManager ThreadManager = new();
    }

    private readonly PerformanceMonitor _performanceMonitor = new();
    private ProcessData _processData = new();
    private RECT _deskRect = new();
}
