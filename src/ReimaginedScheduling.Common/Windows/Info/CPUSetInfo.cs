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
        string[] description = ["", "", ""];
        if (PCoreEfficiencyIndex < 3)
        {
            if (PCoreEfficiencyIndex >= 2) description[PCoreEfficiencyIndex - 2] = "LPE";
            if (PCoreEfficiencyIndex >= 1) description[PCoreEfficiencyIndex - 1] = "E";
            if (PCoreEfficiencyIndex >= 0) description[PCoreEfficiencyIndex - 0] = "P";
        }

        var cpustr = "";
        foreach (var cs in CoreSets)
        {
            cpustr += $"|{cs.Anonymous.CpuSet.Group,-5}";
            cpustr += $"|{cs.Anonymous.CpuSet.Id,-3}";
            cpustr += $"|{cs.Anonymous.CpuSet.CoreIndex,-3}";
            cpustr += $"|{cs.Anonymous.CpuSet.LogicalProcessorIndex,-3}";
            cpustr += $"|{description[cs.Anonymous.CpuSet.EfficiencyClass],-4}";
            cpustr += $"|{cs.Anonymous.CpuSet.Anonymous2.SchedulingClass,-8}";
            cpustr += "|\n";
        }

        var headerstr = "|Group|ID |PI |LI |Type|Priority|";
        var headersplitstr = new string('-', headerstr.Length);
        var str = 
            headersplitstr + '\n' +
            headerstr + '\n' +
            headersplitstr + '\n' +
            cpustr +
            headersplitstr + '\n' +
            $"{PhysicalPCores.Count}P + {ECores.Count}E";
        return str;
    }
}
