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

    public static Distribution Generate(uint mainTID, List<uint> TIDs)
    {
        var PhyPCores = CPUSetInfo.PhysicalPCores[0..];
        var HTs = CPUSetInfo.HyperThreads[0..];
        var ECores = CPUSetInfo.ECores[0..];

        var distribution = new Distribution
        {
            PAttributions =
            [
                new("GameThread", mainTID, PhyPCores[0])
            ]
        };
        var PIndex = distribution.PAttributions.Count;
        
        var thinfos = ThreadInfo.PackWithName(TIDs);
        bool arrangeThread(Regex[] regexs)
        {
            var ret = false;
            foreach (var r in regexs)
            {
                var thifs = thinfos.Where(x => r.IsMatch(x.Name));
                foreach (var thif in thifs)
                {
                    if (PIndex < PhyPCores.Count)
                    {
                        distribution.PAttributions.Add(new(thif.Name, thif.TID, PhyPCores[PIndex]));
                    }
                }
                if (thifs.Any())
                {
                    PIndex++;
                    ret = true;
                }
            }
            return ret;
        }
        List<Regex[]> regs = [_UEThreadNames, _UnityThreadNames, _OtherThreadNames];
        foreach (var reg in regs)
        {
            if (arrangeThread(reg))
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
                pi.SetPriority(priority);
                var cpuidstr = "";
                if (distribution.SharedPhyCPUIDs.Count > 0)
                {
                    pi.SetCpuSets([..distribution.SharedPhyCPUIDs, ..distribution.SharedHTCPUIDs]);
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
                    ti.SetPriority(priority);
                    var coreIndex = pa.CPUID - CPUSetInfo.BeginCPUID;
                    ti.SetIdealNumber(coreIndex);
                    ti.SetCpuSets([pa.CPUID]);
                    
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
