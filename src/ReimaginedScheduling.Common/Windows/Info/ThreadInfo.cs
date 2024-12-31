using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Common.Windows.Info;

public class ThreadInfo
{
    public uint TID { get; private set; }

    public ThreadInfo()
    {
        TID = PInvoke.GetCurrentProcessId();
        _hThread = PInvoke.GetCurrentThread_SafeHandle();
    }
    public ThreadInfo(uint TID)
    {
        this.TID = TID;
        _hThread = PInvoke.OpenThread_SafeHandle(
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION, false, TID);
    }

    public bool IsValid => !IsInvalid;
    public bool IsInvalid => _hThread?.IsInvalid ?? true;

    public int CurrentPriority
    {
        get => PInvoke.GetThreadPriority(_hThread);
        set => PInvoke.SetThreadPriority(_hThread, (THREAD_PRIORITY)value);
    }

    public nuint CurrentMask
    {
        get
        {
            PInvoke.GetThreadGroupAffinity(_hThread, out var groupAffinity);
            return groupAffinity.Mask;
        }
    }

    public uint[] CurrentCpuSets
    {
        get
        {
            var cpuids = new uint[CurrentCpuSetCount];
            PInvoke.GetThreadSelectedCpuSets(_hThread, cpuids, out _);
            return cpuids;
        }
        set => PInvoke.SetThreadSelectedCpuSets(_hThread, value);
    }

    public uint CurrentCpuSetCount
    {
        get
        {
            PInvoke.GetThreadSelectedCpuSets(_hThread, new(), out var requiredIdCount);
            return requiredIdCount;
        }
    }

    public int CurrentCpuSetMaskCount
    {
        get
        {
            PInvoke.GetThreadSelectedCpuSetMasks(_hThread, new(), out var requiredMaskCount);
            return requiredMaskCount;
        }
    }

    public ulong CurrentCycleTime
    {
        get
        {
            PInvoke.QueryThreadCycleTime(_hThread, out var cycleTime);
            return cycleTime;
        }
    }

    public unsafe string CurrentName
    {
        get
        {
            PInvoke.GetThreadDescription(_hThread, out var ppszThreadDescription);
            var name = ppszThreadDescription.ToString() ?? "";
            PInvoke.LocalFree(new HLOCAL(ppszThreadDescription));
            return name;
        }
    }

    public uint CurrentIdealNumber
    {
        get
        {
            PInvoke.GetThreadIdealProcessorEx(_hThread, out var lpIdealProcessor);
            return lpIdealProcessor.Number;
        }
        set => PInvoke.SetThreadIdealProcessor(_hThread, value);
    }

    private readonly SafeFileHandle? _hThread;
}
