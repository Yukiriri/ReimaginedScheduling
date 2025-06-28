using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace ReimaginedScheduling.Lib.Windows.Info;

public static class CpuSetInfo
{
    private static SYSTEM_CPU_SET_INFORMATION[] coreSets { get; set; } = [];
    public static List<uint> PCores { get; private set; }
    public static List<uint> ECores { get; private set; }
    public static List<uint> PECores { get; private set; }
    public static List<uint[]> groupedPECores { get; private set; }
    public static List<uint> physicalPCores { get; private set; }
    public static List<uint> hyperThreads { get; private set; }
    public static List<uint> physicalPECores { get; private set; }
    public static uint beginCpuId { get; private set; }
    private static uint maxEfficiencyClass { get; set; }
    public static bool isPCoreOnly => maxEfficiencyClass == 0;

    static CpuSetInfo()
    {
        unsafe
        {
            PInvoke.GetSystemCpuSetInformation(null, 0, out var arrSize, null);
            if (arrSize > 0)
            {
                var arr = new SYSTEM_CPU_SET_INFORMATION[arrSize / sizeof(SYSTEM_CPU_SET_INFORMATION)];
                fixed (SYSTEM_CPU_SET_INFORMATION* ptr = arr)
                {
                    if (PInvoke.GetSystemCpuSetInformation(ptr, arrSize, out _, null))
                    {
                        coreSets = arr;
                    }
                }
            }
        }

        beginCpuId = coreSets.FirstOrDefault().Anonymous.CpuSet.Id;
        maxEfficiencyClass = coreSets.Max(cs => cs.Anonymous.CpuSet.EfficiencyClass);
        PCores = coreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == maxEfficiencyClass)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        ECores = coreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass != maxEfficiencyClass)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        PECores = [..PCores, ..ECores];
        groupedPECores = coreSets
            .GroupBy(cs => cs.Anonymous.CpuSet.CoreIndex)
            .Select(cs => cs.Select(x => x.Anonymous.CpuSet.Id).ToArray())
            .ToList();
        physicalPCores = coreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == maxEfficiencyClass && cs.Anonymous.CpuSet.CoreIndex == cs.Anonymous.CpuSet.LogicalProcessorIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        hyperThreads = coreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == maxEfficiencyClass && cs.Anonymous.CpuSet.CoreIndex != cs.Anonymous.CpuSet.LogicalProcessorIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        physicalPECores = [..physicalPCores, ..ECores];
    }

    public static uint toMask(uint[] cpuIds)
    {
        return cpuIds.Aggregate((mask, next) => mask | 1u << (int)(next - beginCpuId));
    }

    public static string toFormat()
    {
        var core_types = new[]{"", "", ""};
        if (maxEfficiencyClass < core_types.Length)
        {
            if (maxEfficiencyClass >= 2) core_types[maxEfficiencyClass - 2] = "LPE";
            if (maxEfficiencyClass >= 1) core_types[maxEfficiencyClass - 1] = "E";
            if (maxEfficiencyClass >= 0) core_types[maxEfficiencyClass - 0] = "P";
        }

        var tb_header =   "|Group |ID    |PhysicalI|LogicalI |Type  |Priority|\n";
        var tb_fmt_data = "|{0,-6}|{1,-6}|{2,-9   }|{3,-9}   |{4,-6}|{5,-8  }|\n";
        var tb_header_split = new string('-', tb_header.Length - 1) + "\n";
        return "\n"
               + tb_header_split
               + tb_header
               + tb_header_split
               + coreSets.SelectMany(x => string.Format(tb_fmt_data,
                   x.Anonymous.CpuSet.Group,
                   x.Anonymous.CpuSet.Id,
                   x.Anonymous.CpuSet.CoreIndex,
                   x.Anonymous.CpuSet.LogicalProcessorIndex,
                   core_types[x.Anonymous.CpuSet.EfficiencyClass],
                   x.Anonymous.CpuSet.Anonymous2.SchedulingClass))
               + tb_header_split
               + $"{physicalPCores.Count}P + {ECores.Count}E";
    }
}
