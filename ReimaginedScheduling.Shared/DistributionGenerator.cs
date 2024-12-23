using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class DistributionGenerator
{
    public struct Attribution(uint TID, string Name, uint[] CPUIDs)
    {
        public uint TID = TID;
        public string Name = Name;
        public uint[] CPUIDs = CPUIDs;
    }
    public struct Distribution
    {
        public List<Attribution> PAttributions;
        public List<uint> SharedCPUIDs;
    }
    private static readonly Regex[] _UEThreadNames = 
    [
        new Regex(@"(RenderThread \d)|(RHISubmissionThread)"),
        new Regex(@"RHIThread"),
        new Regex(@"Foreground Worker #0(?!.+)"),
        new Regex(@"Foreground Worker #1(?!.+)"),
        new Regex(@"Foreground Worker #2(?!.+)"),
        new Regex(@"Foreground Worker #3(?!.+)"),
        new Regex(@"Foreground Worker #4(?!.+)"),
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
        var distribution = new Distribution
        {
            PAttributions =
            [
                new(mainTID, "GameThread", CPUSetInfo.UniqueCores[0]),
            ]
        };
        var PIndex = distribution.PAttributions.Count;
        
        var thinfos = ProcessUtilities.GetTIDs(PID)
            .Select(x => (TID: x, ti: new ThreadInfo(x)))
            .Where(x => x.ti.IsValid);
        List<Regex[]> regexs = [_UEThreadNames, _UnityThreadNames, _OtherThreadNames];
        foreach (var regs in regexs)
        {
            var isMatched = false;
            foreach (var reg in regs)
            {
                if (PIndex < CPUSetInfo.PhysicalPCores.Count)
                {
                    var mthinfos = thinfos
                        .Where(x => reg.IsMatch(x.ti.CurrentName))
                        .Select(x => new Attribution(x.TID, x.ti.CurrentName, CPUSetInfo.UniqueCores[PIndex]));
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
        if (CPUSetInfo.ECores.Count == 0)
        {
            sharedIndex = Math.Min(sharedIndex, CPUSetInfo.PhysicalPCores.Count / 2);
        }
        distribution.SharedCPUIDs = CPUSetInfo.UniqueCores[sharedIndex..].SelectMany(x => x).ToList();

        return distribution;
    }

    public static bool ToggleScheduling(uint pid, string windowName, Distribution distribution, bool isRedistribution)
    {
        var pi = new ProcessInfo(pid);
        if (pi.IsInvalid)
            return false;
        
        var thstr = "";
        foreach (var pa in distribution.PAttributions)
        {
            var ti = new ThreadInfo(pa.TID);
            if (ti.IsValid)
            {
                var priority = (int)THREAD_PRIORITY.THREAD_PRIORITY_HIGHEST;
                ti.CurrentPriority = priority;
                var idealIndex = pa.CPUIDs[0] - CPUSetInfo.BeginCPUID;
                ti.CurrentIdealNumber = idealIndex;
                var cpuidstr = string.Join(',', pa.CPUIDs.Select(x => x - CPUSetInfo.BeginCPUID));
                ti.CurrentCpuSets = [..pa.CPUIDs];
                
                thstr += $"|{pa.TID,-5}";
                thstr += $"|{pa.Name,-40}";
                thstr += $"|{priority,-8}";
                thstr += $"|{cpuidstr,-7}";
                thstr += "|\n";
            }
        }
        var procstr = "";
        {
            var priority = (uint)PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS;
            pi.CurrentPriority = priority;
            var beginindex = distribution.SharedCPUIDs.First() - CPUSetInfo.BeginCPUID;
            var endindex = distribution.SharedCPUIDs.Last() - CPUSetInfo.BeginCPUID;
            var cpuidstr = $"{beginindex}-{endindex}";
            pi.CurrentCpuSets = distribution.SharedCPUIDs;
            
            procstr += $"|{pid,-5}";
            procstr += $"|{windowName}";
            procstr += $"|{priority,-8}";
            procstr += $"|{cpuidstr,-7}";
            procstr += "|\n";
        }

        var headerstr = $"|ID   |{"Name",-40}|Priority|CPU    |";
        var headersplitstr = new string('-', headerstr.Length);
        var str = "\n" +
            headersplitstr + '\n' +
            headerstr + '\n' +
            headersplitstr + '\n' +
            procstr +
            headersplitstr + '\n' +
            thstr +
            headersplitstr + '\n';
        MyLogger.Info(str);
        return true;
    }
}
