using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        private double GetCPUUsage(string processorID)
        {
            var value = GetCounterValue("""\Processor(*)\% Processor Time""", Pdh.PDH_FMT.PDH_FMT_DOUBLE, new Regex($"{processorID}"));
            return value.Count > 0 ? value.First().Value.doubleValue : 0;
        }

        public double GetCPUUsage(int processorID) => GetCPUUsage($"{processorID}");

        public double GetAllCPUUsage() => GetCPUUsage("_Total");

        public double GetGPUUsage(uint? pid)
        {
            double value = 0;
            foreach (var item in GetCounterValue("""\GPU Engine(*)\Utilization Percentage""", Pdh.PDH_FMT.PDH_FMT_DOUBLE, new Regex($"pid_{pid}.*engtype_(3D|Graphics)")))
            {
                value += item.Value.doubleValue;
            }
            return value;
        }

        public double GetAllGPUUsage() => GetGPUUsage(null);

        public long GetGPUMemUsage(uint? pid)
        {
            long value = 0;
            foreach (var item in GetCounterValue("""\GPU Process Memory(*)\Dedicated Usage""", Pdh.PDH_FMT.PDH_FMT_LARGE, new Regex($"pid_{pid}")))
            {
                value += item.Value.largeValue;
            }
            return value;
        }

        public long GetAllGPUMemUsage() => GetGPUMemUsage(null);

        public Dictionary<int, double> GetProcessThreadUsage(string processName, int maxThread)
        {
            var value = new Dictionary<int, double>();
            for (int i = 0; i < maxThread; i++)
            {
                var v = GetCounterValue($"""\Thread({processName}/{i})\% Processor Time""", Pdh.PDH_FMT.PDH_FMT_DOUBLE);
                value[i] = v.Count > 0 ? v.First().Value.doubleValue : 0;
            }
            return value;
        }

        public Dictionary<int, double> GetProcessThreadID(string processName, int maxThread)
        {
            var value = new Dictionary<int, double>();
            for (int i = 0; i < maxThread; i++)
            {
                var v = GetCounterValue($"""\Thread({processName}/{i})\ID Thread""", Pdh.PDH_FMT.PDH_FMT_DOUBLE);
                value[i] = v.Count > 0 ? v.First().Value.doubleValue : 0;
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

        private Dictionary<string, Pdh.PDH_FMT_COUNTERVALUE> GetCounterValue(string counterName, Pdh.PDH_FMT counterType, [Optional] Regex regex)
        {
            var usage = new Dictionary<string, Pdh.PDH_FMT_COUNTERVALUE>();
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
                            if (regex != null && !regex.IsMatch(item.szName))
                                continue;
                            var value = new Pdh.PDH_FMT_COUNTERVALUE();
                            switch (counterType)
                            {
                                case Pdh.PDH_FMT.PDH_FMT_LARGE: value.largeValue += item.FmtValue.largeValue; break;
                                case Pdh.PDH_FMT.PDH_FMT_DOUBLE: value.doubleValue += item.FmtValue.doubleValue; break;
                            }
                            usage[item.szName] = value;
                        }
                    }
                }
            }
            else
            {
                AddCounter(counterName);
            }
            Update();
            return usage;
        }

        private bool AddCounter(string counterName)
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

        private bool RemoveCounter(string counterName)
        {
            if (_counterList.TryGetValue(counterName, out var hCounter))
            {
                Pdh.PdhRemoveCounter(hCounter);
            }
            return _counterList.Remove(counterName);
        }

        private bool? Update()
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

        private readonly Pdh.SafePDH_HQUERY _hQuery;

        private readonly Dictionary<string, Pdh.SafePDH_HCOUNTER> _counterList = [];

        private long _updateTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() - 500;
        private readonly object _lUpdate = new();
    }
}
