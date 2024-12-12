using ReimaginedScheduling.Services.Utils;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ReimaginedScheduling.Services;

public class GameProcessManager
{
    public GameProcessManager()
    {
        PInvoke.GetWindowRect(PInvoke.GetDesktopWindow(), out _deskRect);
    }

    public bool IsGameProcess()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        PInvoke.GetClientRect(hwnd, out var wndRect);
        if (wndRect.Size != _deskRect.Size)
        {
            var ci = new CURSORINFO();
            if (PInvoke.GetCursorInfo(ref ci) && ci.flags != 0)
                return false;
        }
        unsafe
        {
            uint pid = 0;
            PInvoke.GetWindowThreadProcessId(hwnd, &pid);
            if (pid != 0)
            {
                var gpuUsage = _performanceMonitor.GetGPUUsage(pid);
                var gpuMemMB = _performanceMonitor.GetGPUMemUsage(pid) >> 20;
                if (gpuUsage >= Config.GPUUsageThreshold && gpuMemMB >= Config.GPUMemUsageThreshold)
                    return true;
            }
        }
        return false;
    }

    public void AttachGameProcess()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        if (hwnd == _processData.hWnd)
            return;
        if (!_processData.hWnd.IsNull)
            DettachGameProcess(true);

        // var windowName = new StringBuilder(PInvoke.GetWindowTextLength(hwnd) + 1);
        // if (PInvoke.GetWindowText(hwnd, windowName, windowName.Capacity) == 0)
        // {
        //     //MyLogger.Debug($"跳过无名窗口");
        //     return;
        // }
        // if (PInvoke.GetWindowThreadProcessId(hwnd) == 0)
        // {
        //     MyLogger.Info($"GetWindowThreadProcessId失败");
        //     return;
        // }
        // var hProcess = PInvoke.OpenProcess(
        //     PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION |
        //     PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION |
        //     PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION |
        //     PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        // if (hProcess.IsNull)
        // {
        //     MyLogger.Info($"OpenProcess失败：PID={pid} Error={Win32Error.GetLastError()}");
        //     return;
        // }
        // var imgFileName = new StringBuilder(Kernel32.MAX_PATH);
        // if (Kernel32.GetProcessImageFileName(hProcess, imgFileName, (uint)imgFileName.Capacity) == 0)
        // {
        //     MyLogger.Info($"GetProcessImageFileName失败：PID={pid} Error={Win32Error.GetLastError()}");
        //     return;
        // }
        // var exeName = new Regex(@"(?!.*\\).+(?=\.exe)").Match(imgFileName.ToString()).Value;
        // _processData = new ProcessData(hwnd, windowName.ToString(), exeName);
    }

    private void DettachGameProcess(bool isRestoreProcess)
    {
        if (isRestoreProcess)
        {
            
        }
        _processData.hWnd = HWND.Null;
    }

    public void Update()
    {
        _performanceMonitor.Update();
        if (_processData.hWnd.IsNull)
        {
            Console.Write("等待前台游戏\r");
            return;
        }
        if (!PInvoke.IsWindow(_processData.hWnd))
        {
            DettachGameProcess(false);
            return;
        }

        // ref var manager = ref _processData.ThreadManager;
        // if (manager.AvailablePCoreCount <= 0)
        //     return;
        // manager.Update(_performanceMonitor.GetProcessThreadData(_processData.exeName, Config.ThreadMonitorCount));
        Console.WriteLine($"{_processData.WindowName} exe={_processData.exeName}.exe");
        // Console.WriteLine(manager.ToString());
        // #if !DEBUG
        // foreach (var th in manager.CurrentAttribution)
        // {
        //     if (th.IsNotNull)
        //         SetThreadCPUID(th.TID, th.CPUID);
        // }
        // Kernel32.SetProcessDefaultCpuSets(_processData.hProcess, manager.CurrentSharedCores, (uint)manager.CurrentSharedCores.Length);
        // #endif
    }

    struct ProcessData(HWND hWnd, string WindowName, string exeName)
    {
        public HWND hWnd = hWnd;
        public string WindowName = WindowName;
        public string exeName = exeName;
        // public GameThreadManager ThreadManager = new();
    }

    private RECT _deskRect = new();
    private readonly PerformanceMonitor _performanceMonitor = new();
    private ProcessData _processData = new();
}
