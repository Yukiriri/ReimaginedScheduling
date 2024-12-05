using ReimaginedScheduling.Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReimaginedScheduling.Services;

public class GameThreadManager
{
    public ThreadAttribution[] CurrentAttribution { get; private set; } = [];
    public List<ThreadAttribution>[] CurrentPerAttribution { get; private set; } = [];
    public uint[] CurrentSharedCores { get; private set; } = [];
    public int AvailablePCoreCount { get; private set; } = 0;
    public int MaxAvailablePCoreCount { get; private set; } = 0;
    public struct ThreadAttribution(uint InstanceID, uint TID, uint CPUID, double Usage)
    {
        public uint InstanceID = InstanceID;
        public uint TID = TID;
        public uint CPUID = CPUID;
        public double Usage = Usage;
        public bool IsNull => TID == 0;
        public bool IsNotNull => TID != 0;
    }

    public GameThreadManager()
    {
        MaxAvailablePCoreCount = CPUSetInfo.PhysicalPCoreList.Count;
        AvailablePCoreCount = MaxAvailablePCoreCount;
        if (CPUSetInfo.IsPCoreOnly && AvailablePCoreCount > Config.TypicalPCoreCount)
            AvailablePCoreCount = Math.Min(AvailablePCoreCount / 2, Config.MaxTypicalPCoreCount);
    }

    private uint GetPPCoreID(int index)
    {
        int nextIndex = 0;
        if (index > 0)
        {
            nextIndex = (1 + ((index - 1) % (MaxAvailablePCoreCount - 1))) % MaxAvailablePCoreCount;
        }
        return CPUSetInfo.PhysicalPCoreList[nextIndex];
    }

    public bool Update(PerformanceMonitor.ProcessThreadIDWithUsage[] threads)
    {
        var filteredTh = threads
            // .Where(th => th.ThreadID != 0)
            .Select(th => new ThreadAttribution(th.InstanceID, th.ThreadID, 0, th.Usage))
            .ToArray();
        
        #if DEBUG
        var logContent = "[";
        foreach (var th in filteredTh)
            logContent += $"new({th.InstanceID,-3},{th.TID,-5},{th.Usage:F0}), ";
        logContent += "],";
        MyLogger.Debug(logContent);
        #endif

        if (filteredTh.Length != 0)
        {
            CurrentAttribution = filteredTh;
            CurrentPerAttribution = new List<ThreadAttribution>[AvailablePCoreCount];
            for (int i = 0; i < AvailablePCoreCount; i++)
                CurrentPerAttribution[i] = [];
            for (int i = 0; i < 1; i++)
            {
                var cpuid = GetPPCoreID(i);
                var coreIndex = (cpuid - CPUSetInfo.BeginCPUID) / 2;
                CurrentAttribution[i].CPUID = cpuid;
                var th = CurrentAttribution[i];

                CurrentPerAttribution[coreIndex].Add(new ThreadAttribution(th.InstanceID, th.TID, cpuid, th.Usage));
            }
            CurrentSharedCores = CPUSetInfo.PhysicalPECoreList
                .Where((cpuid, index) => index > 0 && index < AvailablePCoreCount)
                .ToArray();
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        var s = "\n";
        for (int i = 0; i < AvailablePCoreCount; i++)
            s += $"{$"CPU{i * 2}",-12}";
        if (CurrentSharedCores.Length != 0)
            s += $"/ {CurrentSharedCores.First() - CPUSetInfo.BeginCPUID}-{CurrentSharedCores.Last() - CPUSetInfo.BeginCPUID} ({CurrentSharedCores.Length})";

        s += $"\n{Config.ConsoleSplitRow}\n";
        
        for (int row = 0, maxRow = CurrentPerAttribution.Aggregate(0, (max, next) => Math.Max(max, next.Count)); row < maxRow; row++)
        {
            for (int col = 0; col < AvailablePCoreCount; col++)
            {
                if (row < CurrentPerAttribution[col].Count)
                {
                    var th = CurrentPerAttribution[col][row];
                    s += $"{$"{th.TID,-5}({th.Usage:F0}%)",-12}";
                }
                else
                {
                    s += $"{"",-12}";
                }
            }
            s += "\n";
        }
        s += "\n";

        return s;
    }
}
