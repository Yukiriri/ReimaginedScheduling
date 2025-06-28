using System.Diagnostics;
using ReimaginedScheduling.Lib.Windows.Info;

namespace ReimaginedScheduling.Lib.Tool;

public static class ProcessRequire
{
    public static void enableSeDebug() => Process.EnterDebugMode();

    public static void setLastCpu()
    {
        ProcessCpuInfo.getCurrentProcessCpuInfo().currentCpuSets = [CpuSetInfo.PECores.Last()];
    }
}
