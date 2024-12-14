using ReimaginedScheduling.Core.Utils;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ReimaginedScheduling.Core;

public class GameProcessManager
{
    public static (string windowName, uint PID, uint TID) GetForegroundWindowInfos()
    {
        unsafe
        {
            var hwnd = PInvoke.GetForegroundWindow();
            var textLength = PInvoke.GetWindowTextLength(hwnd) + 1;
            var text = new char[textLength];
            string windowName = "";
            fixed (char* textPtr = text)
            {
                if (PInvoke.GetWindowText(hwnd, textPtr, textLength) > 0)
                    windowName = new string(text);
            }
            uint pid = 0;
            var tid = PInvoke.GetWindowThreadProcessId(hwnd, &pid);
            return (windowName, pid, tid);
        }
    }

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

        var (windowName, pid, _) = GetForegroundWindowInfos();
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

    private RECT _deskRect = new();
    private readonly PerformanceMonitor _performanceMonitor = new();
    private ProcessData _processData = new();
}
