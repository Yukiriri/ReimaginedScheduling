using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class ThreadInfo
{
    public ThreadInfo(uint TID)
    {
        _hThread = PInvoke.OpenThread_SafeHandle(
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION, false, TID);
    }

    public bool IsValid => !IsInvalid;
    public bool IsInvalid => _hThread.IsInvalid;

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

    public List<uint> CurrentCpuSets
    {
        get
        {
            var cpuids = new uint[CurrentCpuSetCount];
            PInvoke.GetThreadSelectedCpuSets(_hThread, cpuids, out _);
            return [..cpuids];
        }
        set => PInvoke.SetThreadSelectedCpuSets(_hThread, value.ToArray());
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

    public string CurrentName
    {
        get
        {
            PInvoke.GetThreadDescription(_hThread, out var ppszThreadDescription);
            return ppszThreadDescription.ToString() ?? "";
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

    public ulong CurrentCycleTime
    {
        get
        {
            PInvoke.QueryThreadCycleTime(_hThread, out var cycleTime);
            return cycleTime;
        }
    }

    private readonly SafeFileHandle _hThread = new();
}
