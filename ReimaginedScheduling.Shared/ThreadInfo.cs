using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class ThreadInfo
{
    public static IEnumerable<(uint TID, string Name)> PackWithName(List<uint> TIDs)
    {
        foreach (uint TID in TIDs)
        {
            var ti = new ThreadInfo(TID);
            if (ti.IsValid)
                yield return (TID, ti.GetName());
        }
    }

    public static IEnumerable<(uint TID, ulong CycleTime)> PackWithCycleTime(List<uint> TIDs)
    {
        foreach (uint TID in TIDs)
        {
            var ti = new ThreadInfo(TID);
            if (ti.IsValid)
                yield return (TID, ti.GetCycleTime());
        }
    }

    public ThreadInfo(uint TID)
    {
        _hThread = PInvoke.OpenThread_SafeHandle(
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION, false, TID);
    }

    public bool IsInvalid => _hThread.IsInvalid;
    public bool IsValid => !_hThread.IsInvalid;

    public string GetName()
    {
        PInvoke.GetThreadDescription(_hThread, out var ppszThreadDescription);
        return ppszThreadDescription.ToString() ?? "";
    }

    public int GetPriority() => PInvoke.GetThreadPriority(_hThread);

    public bool SetPriority(int priority) => PInvoke.SetThreadPriority(_hThread, (THREAD_PRIORITY)priority);

    public nuint GetMask()
    {
        PInvoke.GetThreadGroupAffinity(_hThread, out var groupAffinity);
        return groupAffinity.Mask;
    }

    public uint[] GetCpuSets()
    {
        PInvoke.GetThreadSelectedCpuSets(_hThread, new(), out var requiredIdCount);
        var cpuids = new uint[requiredIdCount];
        PInvoke.GetThreadSelectedCpuSets(_hThread, cpuids, out _);
        return cpuids;
    }

    public bool SetCpuSets(List<uint> CPUIDs) => PInvoke.SetThreadSelectedCpuSets(_hThread, CPUIDs.ToArray());

    public int GetCpuSetMaskCount()
    {
        PInvoke.GetThreadSelectedCpuSetMasks(_hThread, new(), out var requiredMaskCount);
        return requiredMaskCount;
    }

    public uint GetIdealNumber()
    {
        PInvoke.GetThreadIdealProcessorEx(_hThread, out var lpIdealProcessor);
        return lpIdealProcessor.Number;
    }

    public bool SetIdealNumber(uint number) => PInvoke.SetThreadIdealProcessor(_hThread, number) != unchecked((uint)-1);

    public ulong GetCycleTime()
    {
        PInvoke.QueryThreadCycleTime(_hThread, out var cycleTime);
        return cycleTime;
    }

    private readonly SafeFileHandle _hThread = new();
}
