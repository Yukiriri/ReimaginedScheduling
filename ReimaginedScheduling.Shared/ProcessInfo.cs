﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class ProcessInfo
{
    private static unsafe List<PROCESSENTRY32> GetProcessList()
    {
        var list = new List<PROCESSENTRY32>();
        using var hsnap = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, 0);
        var pe32 = new PROCESSENTRY32()
        {
            dwSize = (uint)sizeof(PROCESSENTRY32)
        };
        if (PInvoke.Process32First(hsnap, ref pe32)) do
        {
            list.Add(pe32);
        } while (PInvoke.Process32Next(hsnap, ref pe32));
        return list;
    }

    public static unsafe string GetExeName(uint PID)
    {
        var pl = GetProcessList().Where(x => x.th32ProcessID == PID).FirstOrDefault();
        return new string((sbyte*)&pl.szExeFile._0);
    }

    private static unsafe List<THREADENTRY32> GetThreadList()
    {
        var list = new List<THREADENTRY32>();
        using var hsnap = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPTHREAD, 0);
        var te32 = new THREADENTRY32()
        {
            dwSize = (uint)sizeof(THREADENTRY32)
        };
        if (PInvoke.Thread32First(hsnap, ref te32)) do
        {
            list.Add(te32);
        } while (PInvoke.Thread32Next(hsnap, ref te32));
        return list;
    }

    public static List<uint> GetTIDs(uint PID) => GetThreadList().Where(x => x.th32OwnerProcessID == PID).Select(x => x.th32ThreadID).ToList();

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
