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
        return getCounterValues(@"\GPU Engine(*)\Utilization Percentage", PDH_FMT.PDH_FMT_DOUBLE)
            .Where(v => pattern.IsMatch(v.name))
            .Select(x => x.value.Anonymous.doubleValue)
            .Aggregate((sum, next) => sum + next);
    }

    public ulong getAllGpuMemUsage() => getGpuMemUsage(new Regex("."));

    public ulong getGpuMemUsage(uint PID) => getGpuMemUsage(new Regex($"pid_{PID}_"));

    private ulong getGpuMemUsage(Regex pattern)
    {
        return getCounterValues(@"\GPU Process Memory(*)\Dedicated Usage", PDH_FMT.PDH_FMT_LARGE)
            .Where(v => pattern.IsMatch(v.name))
            .Select(x => (ulong)x.value.Anonymous.largeValue)
            .Aggregate((sum, next) => sum + next);
    }

    private unsafe (string name, PDH_FMT_COUNTERVALUE value)[] getCounterValues(string counter_name, PDH_FMT counter_type)
    {
        if (counterList.TryGetValue(counter_name, out var hCounter))
        {
            var arr_size = 0u;
            if (PInvoke.PdhGetFormattedCounterArray(hCounter, counter_type, ref arr_size, out var arr_length, null) == 0x800007D2)
            {
                var arr = new PDH_FMT_COUNTERVALUE_ITEM_W[arr_size / sizeof(PDH_FMT_COUNTERVALUE_ITEM_W)];
                fixed (PDH_FMT_COUNTERVALUE_ITEM_W* ptr = arr)
                {
                    if (PInvoke.PdhGetFormattedCounterArray(hCounter, counter_type, ref arr_size, out _, ptr) == 0)
                    {
                        return [..arr.Take((int)arr_length).Select(x => (x.szName.ToString(), x.FmtValue))];
                    }
                }
            }
        }
        else
        {
            addCounter(counter_name);
        }
        return [];
    }

    public bool update()
    {
        return PInvoke.PdhCollectQueryData(hQuery) == 0;
    }

    private bool addCounter(string counter_name)
    {
        if (!counterList.ContainsKey(counter_name))
        {
            if (PInvoke.PdhAddCounter(hQuery, counter_name, 0, out var phCounter) == 0)
            {
                counterList[counter_name] = phCounter;
                return true;
            }
        }
        return false;
    }

}
