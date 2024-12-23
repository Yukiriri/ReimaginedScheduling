using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class ProcessInfo
{
    public ProcessInfo(uint PID)
    {
        _hProcess = PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION|
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION|
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION|
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION, false, PID);
    }

    public bool IsValid => !_hProcess.IsInvalid;
    public bool IsInvalid => _hProcess.IsInvalid;

    public uint CurrentPriority
    {
        get => PInvoke.GetPriorityClass(_hProcess);
        set => PInvoke.SetPriorityClass(_hProcess, (PROCESS_CREATION_FLAGS)value);
    }

    public nuint CurrentMask
    {
        get
        {
            PInvoke.GetProcessAffinityMask(_hProcess, out var mask, out _);
            return mask;
        }
    }

    public List<uint> CurrentCpuSets
    {
        get
        {
            var cpuids = new uint[CurrentCpuSetCount];
            PInvoke.GetProcessDefaultCpuSets(_hProcess, cpuids, out _);
            return [..cpuids];
        }
        set => PInvoke.SetProcessDefaultCpuSets(_hProcess, value.ToArray());
    }

    public uint CurrentCpuSetCount
    {
        get
        {
            PInvoke.GetProcessDefaultCpuSets(_hProcess, new(), out var requiredIdCount);
            return requiredIdCount;
        }
    }

    public int CurrentCpuSetMaskCount
    {
        get
        {
            PInvoke.GetProcessDefaultCpuSetMasks(_hProcess, new(), out var requiredMaskCount);
            return requiredMaskCount;
        }
    }

    public ulong CurrentCycleTime
    {
        get
        {
            PInvoke.QueryProcessCycleTime(_hProcess, out var cycleTime);
            return cycleTime;
        }
    }

    private readonly SafeFileHandle _hProcess = new();
}
