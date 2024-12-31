using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Common.Windows.Info;

public class ProcessInfo
{
    public uint PID { get; private set; }

    public ProcessInfo()
    {
        PID = PInvoke.GetCurrentProcessId();
        _hProcess = PInvoke.GetCurrentProcess_SafeHandle();
    }
    public ProcessInfo(uint PID)
    {
        this.PID = PID;
        _hProcess = PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION, false, PID);
    }

    public bool IsValid => !IsInvalid;
    public bool IsInvalid => _hProcess?.IsInvalid ?? true;

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

    public uint[] CurrentCpuSets
    {
        get
        {
            var cpuids = new uint[CurrentCpuSetCount];
            PInvoke.GetProcessDefaultCpuSets(_hProcess, cpuids, out _);
            return cpuids;
        }
        set => PInvoke.SetProcessDefaultCpuSets(_hProcess, value);
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

    public static unsafe string GetName(uint PID)
    {
        using var hsnap = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, 0);
        var pe32 = new PROCESSENTRY32()
        {
            dwSize = (uint)sizeof(PROCESSENTRY32)
        };
        if (PInvoke.Process32First(hsnap, ref pe32)) do
        {
            if (pe32.th32ProcessID == PID)
            {
                return new string((sbyte*)&pe32.szExeFile._0);
            }
        } while (PInvoke.Process32Next(hsnap, ref pe32));
        return "";
    }

    public static unsafe List<uint> GetTIDs(uint PID)
    {
        var list = new List<uint>();
        using var hsnap = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPTHREAD, 0);
        var te32 = new THREADENTRY32()
        {
            dwSize = (uint)sizeof(THREADENTRY32)
        };
        if (PInvoke.Thread32First(hsnap, ref te32)) do
        {
            if (te32.th32OwnerProcessID == PID)
            {
                list.Add(te32.th32ThreadID);
            }
        } while (PInvoke.Thread32Next(hsnap, ref te32));
        return list;
    }

    private readonly SafeFileHandle? _hProcess;
}
