using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Lib.Windows.Info;

public class ProcessCpuInfo
{
    private readonly SafeFileHandle hProcess;
    public int processId { get; private set; }
    public bool isInvalid => hProcess.IsInvalid;
    public bool isValid => !isInvalid;

    public ProcessCpuInfo(int process_id)
    {
        processId = process_id;
        hProcess = PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION, false, (uint)processId);
    }

    public static ProcessCpuInfo getCurrentProcessCpuInfo() => new ProcessCpuInfo((int)PInvoke.GetCurrentProcessId());
    
    public uint currentPriority
    {
        get => PInvoke.GetPriorityClass(hProcess);
        set => PInvoke.SetPriorityClass(hProcess, (PROCESS_CREATION_FLAGS)value);
    }

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

    public ulong currentCycleTime
    {
        get
        {
            PInvoke.QueryProcessCycleTime(hProcess, out var cycleTime);
            return cycleTime;
        }
    }

}
