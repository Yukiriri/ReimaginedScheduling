using Windows.Win32;

namespace ReimaginedScheduling.Lib.Windows.Info;

public class ProcessCpuInfo(int process_id) : ProcessBaseInfo(process_id)
{
    public new static ProcessCpuInfo getCurrentProcess() => new((int)PInvoke.GetCurrentProcessId());
    
    public nuint currentCpuMask
    {
        get
        {
            PInvoke.GetProcessAffinityMask(hProcess, out var lpProcessAffinityMask, out _);
            return lpProcessAffinityMask;
        }
        set => PInvoke.SetProcessAffinityMask(hProcess, value);
    }

    public uint[] currentCpuSets
    {
        get
        {
            var cpu_ids = new uint[currentCpuSetCount];
            PInvoke.GetProcessDefaultCpuSets(hProcess, cpu_ids, out _);
            return cpu_ids;
        }
        set => PInvoke.SetProcessDefaultCpuSets(hProcess, value);
    }

    public int currentCpuSetCount
    {
        get
        {
            PInvoke.GetProcessDefaultCpuSets(hProcess, new(), out var requiredIdCount);
            return (int)requiredIdCount;
        }
    }

    public int currentCpuSetMaskCount
    {
        get
        {
            PInvoke.GetProcessDefaultCpuSetMasks(hProcess, new(), out var requiredMaskCount);
            return requiredMaskCount;
        }
    }

}
