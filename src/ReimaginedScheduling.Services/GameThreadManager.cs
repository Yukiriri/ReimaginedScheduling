using ReimaginedScheduling.Services.Utils;
using System;
using System.Linq;

namespace ReimaginedScheduling.Services
{
    public class GameThreadManager
    {
        public GameThreadManager()
        {
            MaxAvailablePCoreCount = CPUSetInfo.PhysicalPCoreList.Count;
            AvailablePCoreCount = MaxAvailablePCoreCount;
            if (CPUSetInfo.IsPCoreOnly && AvailablePCoreCount > Config.TypicalPCoreCount)
                AvailablePCoreCount /= 2;
            CurrentAttribution = new ThreadAttribution[AvailablePCoreCount];
            for (int i = 0; i < CurrentAttribution.Length; i++)
                CurrentAttribution[i] = new();
        }

        public void Append(PerformanceMonitor.ProcessThreadIDWithUsage[] threads)
        {
            var newAttribution = threads
                .Where(th => th.Usage >= Config.ThreadUsageThreshold)
                .OrderByDescending(th => th.Usage)
                .Where((th, index) => index < AvailablePCoreCount)
                .Select((th, index) => new ThreadAttribution((int)th.ThreadID, CPUSetInfo.PhysicalPCoreList[index], th.Usage))
                .ToArray();
            IsNeedUpdate = CurrentAttribution[0].TID != newAttribution[0].TID;
            if (IsNeedUpdate)
            {
                CurrentRestoreThreads = CurrentAttribution
                    .Select((ca, index) => ((index < newAttribution.Length && !newAttribution.Where(na => ca.TID == na.TID).Any()) || (index >= newAttribution.Length)) ? ca : new())
                    .ToArray();
                IsNeedRestore = CurrentRestoreThreads.Where(tid => !tid.IsNull).Any();
                CurrentOverrideThreads = CurrentAttribution
                    .Select((ca, index) => (index < newAttribution.Length && ca.TID != newAttribution[index].TID) ? newAttribution[index] : new())
                    .ToArray();
                IsNeedOverride = CurrentOverrideThreads.Where(tid => !tid.IsNull).Any();
                CurrentAttribution = CurrentAttribution
                    .Select((ca, index) => index < newAttribution.Length ? newAttribution[index] : new())
                    .ToArray();
            }
            else
            {
                CurrentRestoreThreads = [];
                IsNeedRestore = false;
                CurrentOverrideThreads = [];
                IsNeedOverride = false;
            }
            if (CPUSetInfo.IsPCoreOnly && AvailablePCoreCount == MaxAvailablePCoreCount)
            {
                CurrentSharedCores = CPUSetInfo.HyperThreadList
                    .Where((cpuid, index) => (index < newAttribution.Length && newAttribution[index].Usage < Config.ThreadExclusiveThreshold) || (index >= newAttribution.Length))
                    .ToArray();
            }
            else
            {
                CurrentSharedCores = CPUSetInfo.PhysicalPECoreList
                    .Where((cpuid, index) => (index < newAttribution.Length && newAttribution[index].Usage < Config.ThreadExclusiveThreshold) || (index >= newAttribution.Length))
                    .ToArray();
            }
        }

        public void WriteLineCPUID()
        {
            for (int i = 0; i < AvailablePCoreCount; i++)
                Console.Write($"{$"CPU{i * 2}",-7}");
            Console.WriteLine();
        }

        private void WriteLineSplit()
        {
            for (int i = 0; i < AvailablePCoreCount; i++)
                Console.Write($"{"|",-7}");
            Console.WriteLine();
        }

        private void WriteCurrentAttribution()
        {
            foreach (var ca in CurrentAttribution)
                Console.Write($"{ca.TID,-7}");
        }

        private void WriteCurrentSharedCores()
        {
            Console.Write($"{CurrentSharedCores.First() - CPUSetInfo.BeginCPUID}-");
            Console.Write($"{CurrentSharedCores.Last() - CPUSetInfo.BeginCPUID}");
            Console.Write($"({CurrentSharedCores.Length})");
        }

        private void WriteFillThreadSplit(ThreadAttribution[] threads, string fill = "")
        {
            for (int i = 0; i < AvailablePCoreCount; i++)
            {
                var th = threads[i];
                if (!th.IsNull)
                    Console.Write($"{(fill.Length > 0 ? fill : th.TID),-7}");
                else
                    Console.Write($"{"|",-7}");
            }
        }

        public void WriteLineThreadMap()
        {
            if (IsNeedRestore)
            {
                WriteLineSplit();
                WriteFillThreadSplit(CurrentRestoreThreads, "X");
            }
            if (IsNeedOverride)
            {
                if (IsNeedRestore)
                    Console.WriteLine();
                WriteLineSplit();
                WriteFillThreadSplit(CurrentOverrideThreads);
            }
            if (IsNeedRestore || IsNeedOverride)
            {
                WriteCurrentAttribution();
                WriteCurrentSharedCores();
                Console.WriteLine();
            }
        }

        public struct ThreadAttribution
        {
            public int TID = -1;
            public uint CPUID = 0;
            public double Usage = 0;
            public bool IsNull => TID == -1;

            public ThreadAttribution() { }
            public ThreadAttribution(int TID, uint CPUID, double Usage)
            {
                this.TID = TID;
                this.CPUID = CPUID;
                this.Usage = Usage;
            }
        }

        public ThreadAttribution[] CurrentAttribution { get; private set; } = [];
        public bool IsNeedUpdate { get; private set; } = false;
        public ThreadAttribution[] CurrentRestoreThreads { get; private set; } = [];
        public bool IsNeedRestore { get; private set; } = false;
        public ThreadAttribution[] CurrentOverrideThreads { get; private set; } = [];
        public bool IsNeedOverride { get; private set; } = false;
        public uint[] CurrentSharedCores { get; private set; } = [];
        public int AvailablePCoreCount { get; private set; } = 0;

        private readonly int MaxAvailablePCoreCount = 0;
    }
}
