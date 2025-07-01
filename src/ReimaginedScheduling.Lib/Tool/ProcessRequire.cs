using System.Diagnostics;
using ReimaginedScheduling.Lib.Windows.Info;

namespace ReimaginedScheduling.Lib.Tool;

public static class ProcessRequire
{
    public static void enableSeDebug() => Process.EnterDebugMode();

    public static void setLastCpu()
    {
        ProcessCpuInfo.getCurrentProcess().currentCpuSets = [CpuSetInfo.PECores.Last()];
    }
}
