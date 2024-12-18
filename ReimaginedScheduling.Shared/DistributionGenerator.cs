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
    private static readonly Regex[] _UEThreadNames = 
    [
        new Regex(@"(?<!Audio.*)(Render|RHISubmission).*(T|t)hread"),
        new Regex(@"RHIThread"),
        new Regex(@"Foreground ?Worker #?1"),
        new Regex(@"Foreground ?Worker #?2"),
        new Regex(@"Foreground ?Worker #?3"),
        new Regex(@"Foreground ?Worker #?4"),
        new Regex(@"Foreground ?Worker #?5"),
    ];
    private static readonly Regex[] _UnityThreadNames = 
    [
        new Regex(@"Unity.*Render.*(T|t)hread"),
        new Regex(@"UnityGfxDevicesThread"),
        new Regex(@"Foreground ?Worker #?1"),
        new Regex(@"Foreground ?Worker #?2"),
        new Regex(@"Foreground ?Worker #?3"),
        new Regex(@"Foreground ?Worker #?4"),
        new Regex(@"Foreground ?Worker #?5"),
    ];

    public static Distribution Generate(uint mainTID, List<uint> TIDs)
    {
        var PCores = CPUSetInfo.PhysicalPCores[0..];
        var HTs = CPUSetInfo.HyperThreads[0..];
        var ECores = CPUSetInfo.ECores[0..];

        var thinfos = ThreadInfo.PackWithName(TIDs);
        var PAttributions = new List<Attribution>()
        {
            new("GameThread", mainTID, PCores[0])
        };
        var PIndex = PAttributions.Count;
        void arrangeThread(Regex[] regexs)
        {
            foreach (var r in regexs)
            {
                var thifs = thinfos.Where(x => r.IsMatch(x.Name));
                foreach (var thif in thifs)
                {
                    if (PIndex < PCores.Count)
                    {
                        PAttributions.Add(new(thif.Name, thif.TID, PCores[PIndex]));
                    }
                }
                if (thifs.Any())
                    PIndex++;
            }
        }
        arrangeThread(_UEThreadNames);
        arrangeThread(_UnityThreadNames);

        var SharedCPUIDs = new List<uint>();
        var sharedIndex = PIndex;
        if (PCores.Count < 12 && ECores.Count == 0)
            sharedIndex = Math.Min(sharedIndex, PCores.Count / 2);
        SharedCPUIDs = [..SharedCPUIDs, ..PCores[sharedIndex..]];
        if (PCores.Count < 12 && ECores.Count == 0 && HTs.Count > 0)
            SharedCPUIDs = [..SharedCPUIDs, ..HTs[sharedIndex..]];
        SharedCPUIDs = [..SharedCPUIDs, ..ECores];

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
                MyLogger.Info($"CPU{coreIndex,-2} = {pa.TID,-5} \"{pa.Name}\"");
            }
        }
        var pi = new ProcessInfo(pid);
        if (pi.IsValid)
        {
            pi.SetPriority((uint)PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS);
            if (distribution.SharedCPUIDs.Count > 0)
            {
                pi.SetCpuSets(distribution.SharedCPUIDs);
                MyLogger.Info($"Shared = CPU{string.Join(" CPU", distribution.SharedCPUIDs.Select(x => x - CPUSetInfo.BeginCPUID))}");
            }
        }
    }
}
