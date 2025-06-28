using ReimaginedScheduling.Lib.Windows.Info.Window;
using ReimaginedScheduling.Lib.Windows.Performance;

namespace ReimaginedScheduling.Pattern;

public class GameProcessManager
{
    private readonly PerformanceMonitor performanceMonitor = new();
    private readonly (int width, int height) desktopSize = new();
    private static int gpuUsageThreshold = 25;
    private static ulong gpuMemUsageThreshold = 1250;

    public GameProcessManager()
    {
        desktopSize = new DesktopWindowInfo().currentSize;
    }

    public bool isGameProcess()
    {
        var wi = new ForegroundWindowInfo();
        if (wi.isInvalid)
            return false;

        var pid = wi.ownerProcessId;
        if (pid != 0)
        {
            var gpuUsage = performanceMonitor.getGpuUsage(pid);
            var gpuMemMB = performanceMonitor.getGpuMemUsage(pid) >> 20;
            if (gpuUsage >= gpuUsageThreshold && gpuMemMB >= gpuMemUsageThreshold)
                return true;
        }
        return false;
    }

    public void tryAddGameProcess()
    {
        
    }

    private void deleteGameProcess(bool isRestoreProcess)
    {
        
    }

    public void update()
    {
        performanceMonitor.update();
    }

}
