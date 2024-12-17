using System;
using ReimaginedScheduling.Shared;

namespace ReimaginedScheduling.CLI.Auto;

public class GameProcessManager
{
    public static int GPUUsageThreshold { get; private set; } = 25;
    public static ulong GPUMemUsageThreshold { get; private set; } = 1250;

    public GameProcessManager()
    {
        var wi = new WindowInfo();
        wi.SetDesktopHWND();
        _desktopSize = wi.GetSize();
    }

    public bool IsGameProcess()
    {
        var wi = new WindowInfo();
        if (!wi.SetForegroundHWND())
            return false;
        
        // var wndSize = wi.GetSize();
        // if (wndSize != _desktopSize)
        // {
        //     var ci = new CURSORINFO();
        //     if (PInvoke.GetCursorInfo(ref ci) && ci.flags != 0)
        //         return false;
        // }
        var pid = wi.GetPID();
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
