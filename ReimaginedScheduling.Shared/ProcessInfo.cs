using System.Collections.Generic;
using Windows.Win32;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Shared;

public class ProcessInfo
{
    public struct ProcessDetailedInfo
    {
        public uint PID;
        public uint Priority;
        public nuint Mask;
        public uint CpuSetsCount;
        public ushort CpuSetMasksCount;
    }
    public struct ThreadDetailedInfo
    {
        public string Name;
        public uint TID;
        public int Priority;
        public nuint Mask;
        public uint CpuSetID;
        public uint CpuSetsCount;
        public ushort CpuSetMasksCount;
        public uint Ideal;
        public ulong CycleTime;
    }

    public ProcessInfo(uint PID)
    {
        _PID = PID;
    }

    public static unsafe (string windowName, uint PID, uint TID) GetFGWindowInfos()
    {
        var hwnd = PInvoke.GetForegroundWindow();
        var textLength = PInvoke.GetWindowTextLength(hwnd) + 1;
        var text = new char[textLength];
        string windowName = "";
        fixed (char* textPtr = text)
        {
            if (PInvoke.GetWindowText(hwnd, textPtr, textLength) > 0)
                windowName = new string(text);
        }
        uint pid = 0;
        var tid = PInvoke.GetWindowThreadProcessId(hwnd, &pid);
        return (windowName, pid, tid);
    }

    public unsafe string GetExeName()
    {
        using var hsnap = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, 0);
        var pe32 = new PROCESSENTRY32()
        {
            dwSize = (uint)sizeof(PROCESSENTRY32)
        };
        if (PInvoke.Process32First(hsnap, ref pe32))
        {
            do
            {
                if (pe32.th32ProcessID == _PID)
                {
                    return new string((sbyte*)&pe32.szExeFile._0);
                }
            } while (PInvoke.Process32Next(hsnap, ref pe32));
        }
        return "";
    }

    public unsafe List<uint> GetTIDs()
    {
        var TIDs = new List<uint>();
        using var hsnap = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPTHREAD, 0);
        var te32 = new THREADENTRY32()
        {
            dwSize = (uint)sizeof(THREADENTRY32)
        };
        if (PInvoke.Thread32First(hsnap, ref te32))
        {
            do
            {
                if (te32.th32OwnerProcessID == _PID)
                {
                    TIDs.Add(te32.th32ThreadID);
                }
            } while (PInvoke.Thread32Next(hsnap, ref te32));
        }
        return TIDs;
    }

    public ProcessDetailedInfo GetProcessDetailedInfo()
    {
        using var hproc = PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION|
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION|
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION|
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION, false, _PID);
        if (hproc == null)
            return new();
        
        var priority = PInvoke.GetPriorityClass(hproc);
        PInvoke.GetProcessAffinityMask(hproc, out var mask, out _);
        PInvoke.GetProcessDefaultCpuSets(hproc, new(), out var requiredIdCount);
        PInvoke.GetProcessDefaultCpuSetMasks(hproc, new(), out var requiredMaskCount);
        return new()
        {
            PID = _PID,
            Priority = priority,
            Mask = mask,
            CpuSetsCount = requiredIdCount,
            CpuSetMasksCount = requiredMaskCount,
        };
    }

    public List<ThreadDetailedInfo> GetThreadDetailedInfos()
    {
        var tdis = new List<ThreadDetailedInfo>();
        foreach (var tid in GetTIDs())
        {
            using var hth = PInvoke.OpenThread_SafeHandle(
                THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION|
                THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION|
                THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION|
                THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION, false, tid);
            if (hth == null)
                continue;
            
            PInvoke.GetThreadDescription(hth, out var ppszThreadDescription);
            var priority = PInvoke.GetThreadPriority(hth);
            PInvoke.GetThreadIdealProcessorEx(hth, out var lpIdealProcessor);
            PInvoke.GetThreadGroupAffinity(hth, out var groupAffinity);
            var cpuids = new uint[1];
            PInvoke.GetThreadSelectedCpuSets(hth, cpuids, out var requiredIdCount);
            PInvoke.GetThreadSelectedCpuSetMasks(hth, new(), out var requiredMaskCount);
            PInvoke.QueryThreadCycleTime(hth, out var cycleTime);
            tdis.Add(new()
            {
                Name = ppszThreadDescription.ToString() ?? "",
                TID = tid,
                Priority = priority,
                Mask = groupAffinity.Mask,
                CpuSetID = cpuids[0],
                CpuSetsCount = requiredIdCount,
                CpuSetMasksCount = requiredMaskCount,
                Ideal = lpIdealProcessor.Number,
                CycleTime = cycleTime,
            });
        }
        return tdis;
    }

    private readonly uint _PID = 0;
}
