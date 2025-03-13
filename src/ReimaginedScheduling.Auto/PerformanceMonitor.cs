using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.System.Performance;

namespace ReimaginedScheduling.Auto;

public class PerformanceMonitor
{
    public PerformanceMonitor()
    {
        PInvoke.PdhOpenQuery(null, 0, out _hQuery);
    }
    ~PerformanceMonitor()
    {
        _ = PInvoke.PdhCloseQuery(_hQuery);
    }

    public double GetAllGPUUsage() => GetGPUUsage(new Regex("engtype_(3D|Graphics)"));

    public double GetGPUUsage(uint PID) => GetGPUUsage(new Regex($"pid_{PID}_.*engtype_(3D|Graphics)"));

    private double GetGPUUsage(Regex pattern)
    {
        return GetCounterValue(@"\GPU Engine(*)\Utilization Percentage", PDH_FMT.PDH_FMT_DOUBLE)
            .Where(v => pattern.IsMatch(v.name))
            .Aggregate(0.0, (sum, next) => sum + next.value.Anonymous.doubleValue);
    }

    public ulong GetAllGPUMemUsage() => GetGPUMemUsage(new Regex("."));

    public ulong GetGPUMemUsage(uint PID) => GetGPUMemUsage(new Regex($"pid_{PID}_"));

    public ulong GetGPUMemUsage(Regex pattern)
    {
        return GetCounterValue(@"\GPU Process Memory(*)\Dedicated Usage", PDH_FMT.PDH_FMT_LARGE)
            .Where(v => pattern.IsMatch(v.name))
            .Aggregate(0ul, (sum, next) => sum + (ulong)next.value.Anonymous.largeValue);
    }

    private unsafe (string name, PDH_FMT_COUNTERVALUE value)[] GetCounterValue(string counterName, PDH_FMT counterType)
    {
        if (_counterList.TryGetValue(counterName, out var hCounter))
        {
            var arrSize = 0u;
            if (PInvoke.PdhGetFormattedCounterArray(hCounter, counterType, ref arrSize, out var arrCount, null) == 0x800007D2)
            {
                var arr = new PDH_FMT_COUNTERVALUE_ITEM_W[arrSize / sizeof(PDH_FMT_COUNTERVALUE_ITEM_W)];
                fixed (PDH_FMT_COUNTERVALUE_ITEM_W* ptr = arr)
                {
                    if (PInvoke.PdhGetFormattedCounterArray(hCounter, counterType, ref arrSize, out _, ptr) == 0)
                    {
                        return [.. arr[0..(int)arrCount].Select(x => (x.szName.ToString(), x.FmtValue))];
                    }
                }
            }
        }
        else
        {
            AddCounter(counterName);
        }
        return [];
    }

    public bool Update()
    {
        return PInvoke.PdhCollectQueryData(_hQuery) == 0;
    }

    private bool AddCounter(string counterName)
    {
        if (!_counterList.ContainsKey(counterName))
        {
            if (PInvoke.PdhAddCounter(_hQuery, counterName, 0, out var hCounter) == 0)
            {
                _counterList[counterName] = hCounter;
                return true;
            }
        }
        return false;
    }

    private readonly nint _hQuery = 0;
    private readonly Dictionary<string, nint> _counterList = [];
}
