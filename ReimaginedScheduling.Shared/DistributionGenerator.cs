using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class DistributionGenerator
{
    public struct Attribution(string Name, uint TID, uint CPUID)
    {
        public string Name = Name;
        public uint TID = TID;
        public uint CPUID = CPUID;
    }
    public struct Distribution
    {
        public List<Attribution> PAttributions;
        public List<uint> SharedCPUIDs;
    }
    private static readonly Regex[] _MergerThreadNames = 
    [
        new Regex(@"(Render|Audio|RHISubmission).*(T|t)hread"),
    ];
    private static readonly Regex[] _IndependenceThreadNames = 
    [
        new Regex("(RHI|GfxDevices)Thread"),
        new Regex("(?<!(P|p)ool.*)Foreground ?Worker"),
    ];

    public static Distribution Generate(uint mainTID, List<uint> TIDs)
    {
        var PCores = CPUSetInfo.PhysicalPCores[0..];
        var HTs = CPUSetInfo.HyperThreads[0..];

        var thinfos = ThreadInfo.PackWithName(TIDs);
        var PAttributions = new List<Attribution>()
        {
            new("GameThread", mainTID, PCores[0])
        };
        var PIndex = PAttributions.Count;
        foreach (var tn in _MergerThreadNames)
        {
            foreach (var thi in thinfos.Where(x => tn.IsMatch(x.Name)))
            {
                if (PIndex < PCores.Count)
                {
                    PAttributions.Add(new(thi.Name, thi.TID, PCores[PIndex]));
                }
            }
            PIndex++;
        }
        foreach (var tn in _IndependenceThreadNames)
        {
            foreach (var thi in thinfos.Where(x => tn.IsMatch(x.Name)))
            {
                if (PIndex < PCores.Count)
                {
                    PAttributions.Add(new(thi.Name, thi.TID, PCores[PIndex]));
                    PIndex++;
                }
            }
        }

        var SharedCPUIDs = new List<uint>();
        var sharedIndex = Math.Min(PAttributions.Count, PCores.Count / 2);
        SharedCPUIDs = [..PCores[sharedIndex..]];
        if (PCores.Count < 12 && HTs.Count > 0)
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
            var ti = new ThreadInfo(pa.TID);
            if (ti.IsValid)
            {
                ti.SetPriority((int)THREAD_PRIORITY.THREAD_PRIORITY_HIGHEST);
                var coreIndex = pa.CPUID - CPUSetInfo.BeginCPUID;
                ti.SetIdealNumber(coreIndex);
                ti.SetCpuSets([pa.CPUID]);
                MyLogger.Info($"CPU{coreIndex,-2} = {pa.TID,-5} [{pa.Name}]");
            }
        }
        var pi = new ProcessInfo(pid);
        if (pi.IsValid)
        {
            pi.SetPriority((uint)PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS);
            if (distribution.SharedCPUIDs.Count > 0)
            {
                pi.SetCpuSets(distribution.SharedCPUIDs);
                MyLogger.Info($"DefaultSets = CPU{string.Join(" CPU", distribution.SharedCPUIDs.Select(x => x - CPUSetInfo.BeginCPUID))}");
            }
        }
    }
}
