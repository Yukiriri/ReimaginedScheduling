using System;
using System.Collections.Generic;
using System.Linq;
using Vanara.PInvoke;

namespace ReimaginedScheduling.Services.Utils;

public static class CPUSetInfo
{
    public static List<Kernel32.SYSTEM_CPU_SET_INFORMATION> CoreSetList { get; } = Kernel32.GetSystemCpuSetInformation().ToList();
    public static List<uint> PCoreList { get; private set; } = [];
    public static List<uint> PhysicalPCoreList { get; private set; } = [];
    public static List<uint> HyperThreadList { get; private set; } = [];
    public static List<uint> ECoreList { get; private set; } = [];
    public static List<uint> PECoreList { get; private set; } = [];
    public static List<uint> PhysicalPECoreList { get; private set; } = [];
    public static uint BeginCPUID { get; private set; }
    public static uint PCoreEfficiencyIndex { get; private set; }
    public static bool IsPCoreOnly => PCoreEfficiencyIndex == 0;

    static CPUSetInfo()
    {
        BeginCPUID = CoreSetList.First().CpuSet.Id;
        PCoreEfficiencyIndex = CoreSetList.Aggregate(0u, (max, next) => Math.Max(max, next.CpuSet.EfficiencyClass));
        CoreSetList.ForEach(csi =>
        {
            var cs = csi.CpuSet;
            var cpuid = cs.Id;
            if (cs.EfficiencyClass == PCoreEfficiencyIndex)
            {
                PCoreList.Add(cpuid);
                if (cs.CoreIndex == cs.LogicalProcessorIndex)
                {
                    PhysicalPCoreList.Add(cpuid);
                    PhysicalPECoreList.Add(cpuid);
                }
                else
                {
                    HyperThreadList.Add(cpuid);
                }
            }
            else
            {
                ECoreList.Add(cpuid);
                PhysicalPECoreList.Add(cpuid);
            }
            PECoreList.Add(cpuid);
        });
    }
}
