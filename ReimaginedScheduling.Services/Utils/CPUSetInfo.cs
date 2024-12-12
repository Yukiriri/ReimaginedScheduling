using System;
using System.Linq;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace ReimaginedScheduling.Services.Utils;

public class CPUSetInfo
{
    private static readonly SYSTEM_CPU_SET_INFORMATION[] CoreSets = [];
    public static uint[] PCores { get; private set; } = [];
    public static uint[] PhysicalPCores { get; private set; } = [];
    public static uint[] HyperThreads { get; private set; } = [];
    public static uint[] ECores { get; private set; } = [];
    public static uint[] PhysicalPECores { get; private set; } = [];
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

        BeginCPUID = CoreSets[0].Anonymous.CpuSet.Id;
        PCoreEfficiencyIndex = CoreSets.Aggregate(0u, (max, next) => Math.Max(max, next.Anonymous.CpuSet.EfficiencyClass));
        PCores = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == PCoreEfficiencyIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToArray();
        ECores = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass != PCoreEfficiencyIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToArray();
        PhysicalPCores = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == PCoreEfficiencyIndex && cs.Anonymous.CpuSet.CoreIndex == cs.Anonymous.CpuSet.LogicalProcessorIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToArray();
        HyperThreads = CoreSets
            .Where(cs => cs.Anonymous.CpuSet.EfficiencyClass == PCoreEfficiencyIndex && cs.Anonymous.CpuSet.CoreIndex != cs.Anonymous.CpuSet.LogicalProcessorIndex)
            .Select(cs => cs.Anonymous.CpuSet.Id)
            .ToArray();
        PhysicalPECores = [.. PhysicalPCores, .. ECores];
    }

    public static uint ToMask(uint[] cpuids)
    {
        uint mask = 0;
        foreach (var id in cpuids)
        {
            mask |= 0b1u << (int)(id - BeginCPUID);
        }
        return mask;
    }

    public static new string ToString()
    {
        string[] description = ["", "", "", ""];
        if (PCoreEfficiencyIndex >= 2) description[PCoreEfficiencyIndex - 2] = "LPE";
        if (PCoreEfficiencyIndex >= 1) description[PCoreEfficiencyIndex - 1] = "E";
        if (PCoreEfficiencyIndex >= 0) description[PCoreEfficiencyIndex - 0] = "P";
        var cpustr = "|G |ID |PI |LI |Type|CPI|\n";
        var splitstr = new string('-', cpustr.Length) + "\n";
        cpustr += splitstr;
        foreach (var cs in CoreSets)
        {
            cpustr += $"|{cs.Anonymous.CpuSet.Group,-2}";
            cpustr += $"|{cs.Anonymous.CpuSet.Id,-3}";
            cpustr += $"|{cs.Anonymous.CpuSet.CoreIndex,-3}";
            cpustr += $"|{cs.Anonymous.CpuSet.LogicalProcessorIndex,-3}";
            cpustr += $"|{description[cs.Anonymous.CpuSet.EfficiencyClass],-4}";
            cpustr += $"|{cs.Anonymous.CpuSet.Anonymous2.SchedulingClass,-3}";
            cpustr += "|\n";
        }
        cpustr += splitstr;
        cpustr += $"{PhysicalPCores.Count()}P + {ECores.Count()}E";
        return cpustr;
    }
}
