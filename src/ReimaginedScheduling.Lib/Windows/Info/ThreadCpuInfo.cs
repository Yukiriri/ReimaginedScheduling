using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace ReimaginedScheduling.Lib.Windows.Info;

public class ThreadCpuInfo(int thread_id) : ThreadBaseInfo(thread_id)
{
    public new static ThreadCpuInfo getCurrentThread() => new((int)PInvoke.GetCurrentThreadId());
    
    public nuint currentCpuMask
    {
        get
        {
            PInvoke.GetThreadGroupAffinity(hThread, out var groupAffinity);
            return groupAffinity.Mask;
        }
        set => PInvoke.SetThreadAffinityMask(hThread, value);
    }

    public uint[] currentCpuSets
    {
        get
        {
            var cpu_ids = new uint[currentCpuSetCount];
            PInvoke.GetThreadSelectedCpuSets(hThread, cpu_ids, out _);
            return cpu_ids;
        }
        set => PInvoke.SetThreadSelectedCpuSets(hThread, value);
    }

    public int currentCpuSetCount
    {
        get
        {
            PInvoke.GetThreadSelectedCpuSets(hThread, new(), out var requiredIdCount);
            return (int)requiredIdCount;
        }
    }

    public int currentCpuSetMaskCount
    {
        get
        {
            PInvoke.GetThreadSelectedCpuSetMasks(hThread, new(), out var requiredIdMaskCount);
            return requiredIdMaskCount;
        }
    }

    public int currentCpuIdealNumber
    {
        get
        {
            PInvoke.GetThreadIdealProcessorEx(hThread, out var lpIdealProcessor);
            return lpIdealProcessor.Number;
        }
        set => PInvoke.SetThreadIdealProcessor(hThread, (uint)value);
    }

}
