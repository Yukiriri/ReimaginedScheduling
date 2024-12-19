using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class DistributionGenerator
{
    public struct Attribution(uint TID, string Name, uint CPUID)
    {
        public uint TID = TID;
        public string Name = Name;
        public uint CPUID = CPUID;
    }
    public struct Distribution
    {
        public List<Attribution> PAttributions;
        public List<uint> SharedPhyCPUIDs;
        public List<uint> SharedHTCPUIDs;
    }
    private static readonly Regex[] _UEThreadNames = 
    [
        new Regex(@"(RenderThread \d)|(RHISubmissionThread)"),
        new Regex(@"RHIThread"),
        new Regex(@"ground Worker #0(?!.+)"),
        new Regex(@"ground Worker #1(?!.+)"),
        new Regex(@"ground Worker #2(?!.+)"),
        new Regex(@"ground Worker #3(?!.+)"),
        new Regex(@"ground Worker #4(?!.+)"),
    ];
    private static readonly Regex[] _UnityThreadNames = 
    [
        new Regex(@"UnityMultiRenderingThread"),
        new Regex(@"UnityGfxDeviceWorker"),
    ];
    private static readonly Regex[] _OtherThreadNames = 
    [
        new Regex(@"Render.*(T|t)hread"),
    ];

    public static Distribution Generate(uint PID, uint mainTID)
    {
        var PhyPCores = CPUSetInfo.PhysicalPCores[0..];
        var HTs = CPUSetInfo.HyperThreads[0..];
        var ECores = CPUSetInfo.ECores[0..];

        var distribution = new Distribution
        {
            PAttributions =
            [
                new(mainTID, "GameThread", PhyPCores[0])
            ]
        };
        var PIndex = distribution.PAttributions.Count;
        
        var thinfos = ProcessInfo.GetTIDs(PID)
            .Select(x => (TID: x, ti: new ThreadInfo(x)))
            .Where(x => x.ti.IsValid);
        List<Regex[]> regexs = [_UEThreadNames, _UnityThreadNames, _OtherThreadNames];
        foreach (var regs in regexs)
        {
            var isMatched = false;
            foreach (var reg in regs)
            {
                if (PIndex < PhyPCores.Count)
                {
                    var mthinfos = thinfos
                        .Where(x => reg.IsMatch(x.ti.CurrentName))
                        .Select(x => new Attribution(x.TID, x.ti.CurrentName, PhyPCores[PIndex]));
                    if (mthinfos.Any())
                    {
                        distribution.PAttributions = [..distribution.PAttributions, ..mthinfos];
                        PIndex++;
                        isMatched = true;
                    }
                }
            }
            if (isMatched)
                break;
        }
        var sharedIndex = PIndex;
        if (PhyPCores.Count < 12 && ECores.Count == 0)
        {
            sharedIndex = Math.Min(sharedIndex, PhyPCores.Count / 2);
            if (HTs.Count > 0)
                distribution.SharedHTCPUIDs = [..HTs[sharedIndex..]];
        }
        distribution.SharedPhyCPUIDs = [..PhyPCores[sharedIndex..], ..ECores];

        return distribution;
    }

    public static void ToggleScheduling(uint pid, string windowName, Distribution distribution, bool isRedistribution)
    {
        var logstr = "\n";
        {
            var str = $"|PID  |{"Name",-40}|Priority|CPU    |";
            var splitstr = new string('-', str.Length);
            logstr += splitstr + '\n';
            logstr += str + '\n';
            logstr += splitstr + '\n';

            var pi = new ProcessInfo(pid);
            if (pi.IsValid)
            {
                var priority = (uint)PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS;
                pi.CurrentPriority = priority;
                var cpuidstr = "";
                if (distribution.SharedPhyCPUIDs.Count > 0)
                {
                    pi.CurrentCpuSets = [..distribution.SharedPhyCPUIDs, ..distribution.SharedHTCPUIDs];
                    cpuidstr = $"{distribution.SharedPhyCPUIDs.First() - CPUSetInfo.BeginCPUID}-{distribution.SharedPhyCPUIDs.Last() - CPUSetInfo.BeginCPUID}";
                }
                
                logstr += $"|{pid,-5}";
                logstr += $"|{windowName}";
                logstr += $"|{priority,-8}";
                logstr += $"|{cpuidstr,-7}";
                logstr += "|\n";
            }
            logstr += splitstr + '\n';
        }
        {
            var str = $"|TID  |{"Name",-40}|Priority|CPU    |";
            var splitstr = new string('-', str.Length);
            logstr += splitstr + '\n';
            logstr += str + '\n';
            logstr += splitstr + '\n';
            foreach (var pa in distribution.PAttributions)
            {
                var ti = new ThreadInfo(pa.TID);
                if (ti.IsValid)
                {
                    var priority = (int)THREAD_PRIORITY.THREAD_PRIORITY_HIGHEST;
                    ti.CurrentPriority = priority;
                    var coreIndex = pa.CPUID - CPUSetInfo.BeginCPUID;
                    ti.CurrentIdealNumber = coreIndex;
                    ti.CurrentCpuSets = [pa.CPUID];
                    
                    logstr += $"|{pa.TID,-5}";
                    logstr += $"|{pa.Name,-40}";
                    logstr += $"|{priority,-8}";
                    logstr += $"|{coreIndex,-7}";
                    logstr += "|\n";
                }
            }
            logstr += splitstr + '\n';
        }
        MyLogger.Info(logstr);
    }
}
