using System.Collections.Generic;
using System.Linq;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace ReimaginedScheduling.Common.Windows.Info;

public class CPUSetInfo
{
    private static readonly SYSTEM_CPU_SET_INFORMATION[] CoreSets = [];
    public static List<uint> PCores { get; private set; } = [];
    public static List<uint> ECores { get; private set; } = [];
    public static List<uint> PhysicalPCores { get; private set; } = [];
    public static List<uint> HyperThreads { get; private set; } = [];
    public static List<uint> PhysicalPECores { get; private set; } = [];
    public static List<uint[]> UniqueCores { get; private set; } = [];
    public static uint BeginCPUID { get; private set; }
    public static uint PCoreEfficiencyIndex { get; private set; }
    public static bool IsPCoreOnly => PCoreEfficiencyIndex == 0;

    static CPUSetInfo()
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
                        CoreSets = arr;
                    }
                }
            }
        }

        BeginCPUID = CoreSets.FirstOrDefault().Anonymous.CpuSet.Id;
        PCoreEfficiencyIndex = CoreSets.Max(cs => cs.Anonymous.CpuSet.EfficiencyClass);
        PCores = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == PCoreEfficiencyIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        ECores = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass != PCoreEfficiencyIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        PhysicalPCores = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == PCoreEfficiencyIndex && cs.Anonymous.CpuSet.CoreIndex == cs.Anonymous.CpuSet.LogicalProcessorIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        HyperThreads = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == PCoreEfficiencyIndex && cs.Anonymous.CpuSet.CoreIndex != cs.Anonymous.CpuSet.LogicalProcessorIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToList();
        PhysicalPECores = [..PhysicalPCores, ..ECores];
        UniqueCores = CoreSets
            .GroupBy(cs => cs.Anonymous.CpuSet.CoreIndex)
            .Select(cs => cs.Select(x => x.Anonymous.CpuSet.Id).ToArray())
            .ToList();
    }

    public static uint ToMask(uint[] CPUIDs)
    {
        uint mask = 0;
        foreach (var id in CPUIDs)
        {
            mask |= 1u << (int)(id - BeginCPUID);
        }
        return mask;
    }

    public static string ToLog()
    {
        var headerstr =     "|Group |ID    |PhysicalI|LogicalI |Type  |Priority|";
        var dataformatstr = "|{0,-6}|{1,-6}|{2,-9   }|{3,-9}   |{4,-6}|{5,-8  }|\n";
        var headersplitstr = new string('-', headerstr.Length);

        var coretypestr = new string[]{"", "", ""};
        if (PCoreEfficiencyIndex < coretypestr.Length)
        {
            if (PCoreEfficiencyIndex >= 2) coretypestr[PCoreEfficiencyIndex - 2] = "LPE";
            if (PCoreEfficiencyIndex >= 1) coretypestr[PCoreEfficiencyIndex - 1] = "E";
            if (PCoreEfficiencyIndex >= 0) coretypestr[PCoreEfficiencyIndex - 0] = "P";
        }

        var datastr = "";
        foreach (var coresets in CoreSets)
        {
            datastr += string.Format(dataformatstr,
                coresets.Anonymous.CpuSet.Group,
                coresets.Anonymous.CpuSet.Id,
                coresets.Anonymous.CpuSet.CoreIndex,
                coresets.Anonymous.CpuSet.LogicalProcessorIndex,
                coretypestr[coresets.Anonymous.CpuSet.EfficiencyClass],
                coresets.Anonymous.CpuSet.Anonymous2.SchedulingClass);
        }

        return "\n" +
            headersplitstr + '\n' +
            headerstr + '\n' +
            headersplitstr + '\n' +
            datastr +
            headersplitstr + '\n' +
            $"{PhysicalPCores.Count}P + {ECores.Count}E";
    }
}
