using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.System.Performance;

namespace ReimaginedScheduling.Lib.Windows.Performance;

public class PerformanceMonitor
{
    private readonly IntPtr hQuery;
    private readonly Dictionary<string, IntPtr> counterList = [];
    
    public PerformanceMonitor()
    {
        PInvoke.PdhOpenQuery(null, 0, out hQuery);
    }
    
    ~PerformanceMonitor()
    {
        PInvoke.PdhCloseQuery(hQuery);
    }

    public double getAllGpuUsage() => getGpuUsage(new Regex("engtype_(3D|Graphics)"));

    public double getGpuUsage(uint PID) => getGpuUsage(new Regex($"pid_{PID}_.*engtype_(3D|Graphics)"));

    private double getGpuUsage(Regex pattern)
    {
        return getCounterValue(@"\GPU Engine(*)\Utilization Percentage", PDH_FMT.PDH_FMT_DOUBLE)
            .Where(v => pattern.IsMatch(v.name))
            .Select(x => x.value.Anonymous.doubleValue)
            .Aggregate((sum, next) => sum + next);
    }

    public ulong getAllGpuMemUsage() => getGpuMemUsage(new Regex("."));

    public ulong getGpuMemUsage(uint PID) => getGpuMemUsage(new Regex($"pid_{PID}_"));

    private ulong getGpuMemUsage(Regex pattern)
    {
        return getCounterValue(@"\GPU Process Memory(*)\Dedicated Usage", PDH_FMT.PDH_FMT_LARGE)
            .Where(v => pattern.IsMatch(v.name))
            .Select(x => (ulong)x.value.Anonymous.largeValue)
            .Aggregate((sum, next) => sum + next);
    }

    private unsafe (string name, PDH_FMT_COUNTERVALUE value)[] getCounterValue(string counterName, PDH_FMT counterType)
    {
        if (counterList.TryGetValue(counterName, out var hCounter))
        {
            var arrSize = 0u;
            if (PInvoke.PdhGetFormattedCounterArray(hCounter, counterType, ref arrSize, out var arrCount, null) == 0x800007D2)
            {
                var arr = new PDH_FMT_COUNTERVALUE_ITEM_W[arrSize / sizeof(PDH_FMT_COUNTERVALUE_ITEM_W)];
                fixed (PDH_FMT_COUNTERVALUE_ITEM_W* ptr = arr)
                {
                    if (PInvoke.PdhGetFormattedCounterArray(hCounter, counterType, ref arrSize, out _, ptr) == 0)
                    {
                        return arr.Take((int)arrCount)
                            .Select(x => (x.szName.ToString(), x.FmtValue))
                            .ToArray();
                    }
                }
            }
        }
        else
        {
            addCounter(counterName);
        }
        return [];
    }

    public bool update()
    {
        return PInvoke.PdhCollectQueryData(hQuery) == 0;
    }

    private bool addCounter(string counterName)
    {
        if (!counterList.ContainsKey(counterName))
        {
            if (PInvoke.PdhAddCounter(hQuery, counterName, 0, out var phCounter) == 0)
            {
                counterList[counterName] = phCounter;
                return true;
            }
        }
        return false;
    }

}
