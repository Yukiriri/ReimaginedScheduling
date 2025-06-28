using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Lib.Windows.Info;

public class ThreadCpuInfo
{
    private readonly SafeFileHandle hThread;
    public int threadId { get; private set; }
    public bool isInvalid => hThread.IsInvalid;
    public bool isValid => !isInvalid;

    public ThreadCpuInfo(int thread_id)
    {
        threadId = thread_id;
        hThread = PInvoke.OpenThread_SafeHandle(
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION, false, (uint)threadId);
    }

    public static ThreadCpuInfo getCurrentThreadCpuInfo() => new ThreadCpuInfo((int)PInvoke.GetCurrentThreadId());
    
    public int currentPriority
    {
        get => PInvoke.GetThreadPriority(hThread);
        set => PInvoke.SetThreadPriority(hThread, (THREAD_PRIORITY)value);
    }

    public unsafe nuint currentCpuMask
    {
        get
        {
            PInvoke.GetThreadGroupAffinity(hThread, out var groupAffinity);
            return groupAffinity.Mask;
        }
        set => PInvoke.SetThreadGroupAffinity(hThread, new GROUP_AFFINITY{Group = 0, Mask = value}, null);
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

    public ulong currentCycleTime
    {
        get
        {
            PInvoke.QueryThreadCycleTime(hThread, out var cycleTime);
            return cycleTime;
        }
    }

    public unsafe string currentName
    {
        get
        {
            PInvoke.GetThreadDescription(hThread, out var ppszThreadDescription);
            var name = ppszThreadDescription.ToString() ?? "";
            PInvoke.LocalFree(new(ppszThreadDescription));
            return name;
        }
    }

}
