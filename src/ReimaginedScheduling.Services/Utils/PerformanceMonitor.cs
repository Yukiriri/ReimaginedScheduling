using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vanara.InteropServices;
using Vanara.PInvoke;

namespace ReimaginedScheduling.Services.Utils
{
    public class PerformanceMonitor
    {
        public PerformanceMonitor()
        {
            Pdh.PdhOpenQuery(phQuery: out _hQuery);
        }
        ~PerformanceMonitor()
        {
            Pdh.PdhCloseQuery(_hQuery);
        }

        public double GetAllCPUUsage() => GetCPUUsage("_Total");

        public double GetCPUUsage(uint processorID) => GetCPUUsage($"{processorID}");

        private double GetCPUUsage(string pattern)
        {
            return GetCounterValue("""\Processor(*)\% Processor Time""", Pdh.PDH_FMT.PDH_FMT_DOUBLE)
                .Where(v => v.name.Contains(pattern))
                .Aggregate(0.0, (sum, next) => sum + next.value.doubleValue);
        }

        public double GetAllGPUUsage() => GetGPUUsage(new Regex("."));

        public double GetGPUUsage(uint PID) => GetGPUUsage(new Regex($"pid_{PID}_.*engtype_(3D|Graphics)"));

        private double GetGPUUsage(Regex pattern)
        {
            return GetCounterValue("""\GPU Engine(*)\Utilization Percentage""", Pdh.PDH_FMT.PDH_FMT_DOUBLE)
                .Where(v => pattern.IsMatch(v.name))
                .Aggregate(0.0, (sum, next) => sum + next.value.doubleValue);
        }

        public ulong GetAllGPUMemUsage() => GetGPUMemUsage("pid");

        public ulong GetGPUMemUsage(uint PID) => GetGPUMemUsage($"pid_{PID}_");

        public ulong GetGPUMemUsage(string pattern)
        {
            return GetCounterValue("""\GPU Process Memory(*)\Dedicated Usage""", Pdh.PDH_FMT.PDH_FMT_LARGE)
                .Where(v => v.name.Contains(pattern))
                .Aggregate(0ul, (sum, next) => sum + (ulong)next.value.largeValue);
        }

        public ProcessThreadUsage[] GetProcessThreadUsage(string processName, int maxThread)
        {
            var value = new ProcessThreadUsage[maxThread];
            for (int i = 0; i < maxThread; i++)
            {
                value[i].InstanceID = (uint)i;
                value[i].Usage = GetCounterValue($"""\Thread({processName}/{i})\% Processor Time""", Pdh.PDH_FMT.PDH_FMT_DOUBLE).DefaultIfEmpty().First().value.doubleValue;
            }
            return value;
        }

        public ProcessThreadID[] GetProcessThreadID(string processName, int maxThread)
        {
            var value = new ProcessThreadID[maxThread];
            for (int i = 0; i < maxThread; i++)
            {
                value[i].InstanceID = (uint)i;
                value[i].ThreadID = (uint)GetCounterValue($"""\Thread({processName}/{i})\ID Thread""", Pdh.PDH_FMT.PDH_FMT_LONG).DefaultIfEmpty().First().value.longValue;
            }
            return value;
        }

        public ProcessThreadIDWithUsage[] GetProcessThreadIDWithUsage(string processName, int maxThread)
        {
            var thu = GetProcessThreadUsage(processName, maxThread);
            var thid = GetProcessThreadID(processName, maxThread);
            var value = new ProcessThreadIDWithUsage[maxThread];
            for (int i = 0; i < maxThread; i++)
            {
                value[i].ThreadID = thid[i].ThreadID;
                value[i].Usage = thu[i].Usage;
            }
            return value;
        }

        public void ClearProcessThreadMonitor(string processName, int maxThread)
        {
            for (int i = 0; i < maxThread; i++)
            {
                RemoveCounter($"""\Thread({processName}/{i})\% Processor Time""");
                RemoveCounter($"""\Thread({processName}/{i})\ID Thread""");
            }
        }

        private IEnumerable<(string name, Pdh.PDH_FMT_COUNTERVALUE value)> GetCounterValue(string counterName, Pdh.PDH_FMT counterType)
        {
            if (_counterList.TryGetValue(counterName, out var hCounter))
            {
                var arrSize = 0u;
                if (Pdh.PdhGetFormattedCounterArray(hCounter, counterType, ref arrSize, out var arrCount) == Win32Error.PDH_MORE_DATA)
                {
                    using var mem = new SafeHGlobalHandle(arrSize);
                    if (Pdh.PdhGetFormattedCounterArray(hCounter, counterType, ref arrSize, out arrCount, mem).Succeeded)
                    {
                        foreach (var item in mem.ToArray<Pdh.PDH_FMT_COUNTERVALUE_ITEM>((int)arrCount))
                        {
                            yield return (item.szName, item.FmtValue);
                        }
                    }
                }
            }
            else
            {
                AddCounter(counterName);
            }
        }

        public bool? Update()
        {
            lock (_lUpdate)
            {
                var t = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                if (t - _updateTime >= 500)
                {
                    _updateTime = t;
                    return Pdh.PdhCollectQueryData(_hQuery).Succeeded;
                }
                return null;
            }
        }

        private bool AddCounter(string counterName)
        {
            lock (_lAddC)
            {
                if (!_counterList.ContainsKey(counterName))
                {
                    if (Pdh.PdhAddCounter(_hQuery, counterName, 0, out var hCounter).Succeeded)
                    {
                        _counterList[counterName] = hCounter;
                        return true;
                    }
                }
                return false;
            }
        }

        private bool RemoveCounter(string counterName)
        {
            lock (_lRemoveC)
            {
                if (_counterList.TryGetValue(counterName, out var hCounter))
                {
                    Pdh.PdhRemoveCounter(hCounter);
                }
                return _counterList.Remove(counterName);
            }
        }

        public struct ProcessThreadUsage(uint InstanceID, double Usage)
        {
            public uint InstanceID = InstanceID;
            public double Usage = Usage;
        }
        public struct ProcessThreadID(uint InstanceID, uint ThreadID)
        {
            public uint InstanceID = InstanceID;
            public uint ThreadID = ThreadID;
        }
        public struct ProcessThreadIDWithUsage(uint ThreadID, double Usage)
        {
            public uint ThreadID = ThreadID;
            public double Usage = Usage;
        }

        private readonly Pdh.SafePDH_HQUERY _hQuery;
        private readonly Dictionary<string, Pdh.SafePDH_HCOUNTER> _counterList = [];
        private long _updateTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() - 500;
        private readonly object _lAddC = new();
        private readonly object _lRemoveC = new();
        private readonly object _lUpdate = new();
    }
}
