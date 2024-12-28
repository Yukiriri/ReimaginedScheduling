using ReimaginedScheduling.Common.Windows.Device;
using ReimaginedScheduling.Common.Windows.Info;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ReimaginedScheduling.CLI.Auto;

public class GameProcessManager
{
    public static int GPUUsageThreshold { get; private set; } = 25;
    public static ulong GPUMemUsageThreshold { get; private set; } = 1250;

    public GameProcessManager()
    {
        var wi = new WindowInfo(PInvoke.GetDesktopWindow());
        _desktopSize = wi.CurrentSize;
    }

    public bool IsGameProcess()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        if (!hwnd.IsNull)
            return false;

        var wi = new WindowInfo(hwnd);
        var wndSize = wi.CurrentSize;
        if (wndSize != _desktopSize)
        {
            var ci = new CURSORINFO();
            if (PInvoke.GetCursorInfo(ref ci) && ci.flags != 0)
                return false;
        }
        var pid = wi.CurrentPID;
        if (pid != 0)
        {
            var gpuUsage = _performanceMonitor.GetGPUUsage(pid);
            var gpuMemMB = _performanceMonitor.GetGPUMemUsage(pid) >> 20;
            if (gpuUsage >= GPUUsageThreshold && gpuMemMB >= GPUMemUsageThreshold)
                return true;
        }
        return false;
    }

    public void TryAttachGameProcess()
    {
        
    }

    private void AttachGameProcess()
    {
        
    }

    private void DettachGameProcess(bool isRestoreProcess)
    {
        
    }

    public void Update()
    {
        _performanceMonitor.Update();
    }

    private readonly (int Width, int Height) _desktopSize = new();
    private readonly PerformanceMonitor _performanceMonitor = new();
}
