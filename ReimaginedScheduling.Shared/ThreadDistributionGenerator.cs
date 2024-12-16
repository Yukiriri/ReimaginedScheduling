using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class ThreadDistributionGenerator
{
    public struct Attribution
    {
        public string Name;
        public uint TID;
        public uint CPUID;
    }
    public struct Distribution
    {
        public List<Attribution> PAttributions;
        public List<uint> SharedCPUIDs;
    }
    private static readonly Regex[] _emphasisThreadNames = 
    [
        new Regex(@"(?<!Audio.*)Render ?(T|t)hread"),
        new Regex("(RHI|GfxDevices)Thread"),
        new Regex("Foreground ?Worker"),
    ];
    private static readonly (int PCount, int MinSharedPIndex)[] _PSharedTable = 
    [
        (2, 0),
        (4, 2),
        (6, 3),
        (8, 3),
        (10, 3),
        (12, 6),
        (16, 8),
    ];

    public static Distribution Generate(uint mainTID, List<ProcessInfo.ThreadDetailedInfo> TDIs)
    {
        var PCores = CPUSetInfo.PhysicalPCores[0..];
        var HTs = CPUSetInfo.HyperThreads[0..];
        var (PCount, MinSharedPIndex) = _PSharedTable
            .Aggregate(_PSharedTable[0], (sel, next) => PCores.Count >= next.PCount ? next : sel);

        var PAttributions = new List<Attribution>()
        {
            new(){Name = "GameThread", TID = mainTID, CPUID = PCores[0]}
        };
        foreach (var etn in _emphasisThreadNames)
        {
            foreach (var tdi in TDIs.Where(x => etn.IsMatch(x.Name)))
            {
                if (PAttributions.Count < PCores.Count)
                {
                    PAttributions.Add(new(){Name = tdi.Name, TID = tdi.TID, CPUID = PCores[PAttributions.Count]});
                }
            }
        }

        var SharedCPUIDs = new List<uint>();
        var sharedIndex = PAttributions.Count;
        if (CPUSetInfo.IsPCoreOnly)
            sharedIndex = Math.Min(MinSharedPIndex, sharedIndex);
        SharedCPUIDs = [..PCores[sharedIndex..]];
        if (CPUSetInfo.IsPCoreOnly && HTs.Count > 0)
            SharedCPUIDs = [..SharedCPUIDs, ..HTs[sharedIndex..]];
        SharedCPUIDs = [..SharedCPUIDs, ..CPUSetInfo.ECores];

        return new()
        {
            PAttributions = PAttributions,
            SharedCPUIDs = SharedCPUIDs,
        };
    }

    public static void ToggleScheduling(uint pid, Distribution distribution, bool isRedistribution)
    {
        foreach (var pa in distribution.PAttributions)
        {
            using var hth = PInvoke.OpenThread_SafeHandle(
                THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION|
                THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION|
                THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION|
                THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION, false, pa.TID);
            if (hth == null)
                continue;
            
            PInvoke.SetThreadPriority(hth, THREAD_PRIORITY.THREAD_PRIORITY_HIGHEST);
            var coreIndex = pa.CPUID - CPUSetInfo.BeginCPUID;
            PInvoke.SetThreadIdealProcessor(hth, coreIndex);
            PInvoke.SetThreadSelectedCpuSets(hth, [pa.CPUID]);
            MyLogger.Info($"[{pa.TID,-5}, {pa.Name,-20}] = CPU{coreIndex,-2}");
        }
        if (distribution.SharedCPUIDs.Count > 0)
        {
            using var hproc = PInvoke.OpenProcess_SafeHandle(
                PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION|
                PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION|
                PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION|
                PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION, false, pid);
            if (hproc == null)
                return;
            
            PInvoke.SetPriorityClass(hproc, PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS);
            PInvoke.SetProcessDefaultCpuSets(hproc, distribution.SharedCPUIDs.ToArray());
            MyLogger.Info($"DefaultSets = CPU{string.Join(" CPU", distribution.SharedCPUIDs.Select(x => x - CPUSetInfo.BeginCPUID))}");
        }
    }
}
