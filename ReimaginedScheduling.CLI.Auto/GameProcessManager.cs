using System;
using ReimaginedScheduling.Shared;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ReimaginedScheduling.CLI.Auto;

public class GameProcessManager
{
    public static int GPUUsageThreshold { get; private set; } = 25;
    public static ulong GPUMemUsageThreshold { get; private set; } = 1250;

    public GameProcessManager()
    {
        PInvoke.GetClientRect(PInvoke.GetDesktopWindow(), out _desktopRect);
    }

    public bool IsGameProcess()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        PInvoke.GetClientRect(hwnd, out var wndRect);
        if (wndRect.Size != _desktopRect.Size)
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
                if (gpuUsage >= GPUUsageThreshold && gpuMemMB >= GPUMemUsageThreshold)
                    return true;
            }
        }
        return false;
    }

    public void TryAttachGameProcess()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        if (hwnd == _processData.hWnd)
            return;
        if (!_processData.hWnd.IsNull)
            DettachGameProcess(true);
        AttachGameProcess();
    }

    private void AttachGameProcess()
    {
        var (windowName, pid, _) = ProcessInfo.GetFGWindowInfos();
        if (windowName.Length == 0)
        {
            //MyLogger.Debug($"跳过无名窗口");
            return;
        }
        if (pid == 0)
        {
            MyLogger.Info($"GetWindowThreadProcessId失败");
            return;
        }
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

        Console.WriteLine($"{_processData.WindowName} exe={_processData.exeName}.exe");
    }

    struct ProcessData
    {
        public HWND hWnd;
        public string WindowName;
        public string exeName;
    }

    private readonly RECT _desktopRect = new();
    private readonly PerformanceMonitor _performanceMonitor = new();
    private ProcessData _processData = new();
}
