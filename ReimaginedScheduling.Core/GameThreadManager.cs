using ReimaginedScheduling.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.Kernel;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Core;

public class GameThreadManager
{
    public static uint OpenProcessAccess => (uint)(
        PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION|
        PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION|
        PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION|
        PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION);
    public static uint OpenThreadAccess => (uint)(
        THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION|
        THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION|
        THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION|
        THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION);
    private static readonly Regex[] EmphasisThreadNames = 
    [
        new Regex(@"Render ?(T|t)hread"), 
        new Regex("RHIThread"), 
        new Regex("Foreground Worker"),
    ];
    private static readonly (uint PCount, bool HasE)[] ThreadDistributionThresholdTable = 
    [
        (4, false),
        (4, true),
        (6, false),
        (6, true),
        (8, false),
        (8, true),
        (10, false),
        (12, false),
        (16, false),
    ];
    private static readonly (uint PCount, bool HasE) CurrentThreadDistributionThreshold = ThreadDistributionThresholdTable
        .Aggregate(ThreadDistributionThresholdTable[0], (sel, next) => ((CPUSetInfo.PhysicalPCores.Count > next.PCount) && (CPUSetInfo.HyperThreads.Count > 0 == next.HasE)) ? next : sel);

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
    }

    public GameThreadManager(uint PID, uint MainTID)
    {
        _PID = PID;
        _MainTID = MainTID;
    }

    public string GetExeName()
    {
        unsafe
        {
            var hsnap = PInvoke.CreateToolhelp32Snapshot(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPPROCESS, 0);
            var pe32 = new PROCESSENTRY32()
            {
                dwSize = (uint)sizeof(PROCESSENTRY32)
            };
            if (PInvoke.Process32First(hsnap, &pe32))
            {
                do
                {
                    if (pe32.th32ProcessID == _PID)
                    {
                        return new string((sbyte*)&pe32.szExeFile._0);
                    }
                } while (PInvoke.Process32Next(hsnap, &pe32));
            }
            PInvoke.CloseHandle(hsnap);
        }
        return "";
    }

    public List<uint> GetTIDs()
    {
        var TIDs = new List<uint>();
        unsafe
        {
            var hsnap = PInvoke.CreateToolhelp32Snapshot(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPTHREAD, 0);
            var te32 = new THREADENTRY32()
            {
                dwSize = (uint)sizeof(THREADENTRY32)
            };
            if (PInvoke.Thread32First(hsnap, &te32))
            {
                do
                {
                    if (te32.th32OwnerProcessID == _PID)
                    {
                        TIDs.Add(te32.th32ThreadID);
                    }
                } while (PInvoke.Thread32Next(hsnap, &te32));
            }
            PInvoke.CloseHandle(hsnap);
        }
        return TIDs;
    }

    public ProcessDetailedInfo? GetProcessDetailedInfo()
    {
        unsafe
        {
            var hproc = PInvoke.OpenProcess((PROCESS_ACCESS_RIGHTS)OpenProcessAccess, false, _PID);
            if (hproc.IsNull)
                return null;
            
            var priority = PInvoke.GetPriorityClass(hproc);
            nuint mask = 0, mask2;
            PInvoke.GetProcessAffinityMask(hproc, &mask, &mask2);
            uint cpuidsCount = 0;
            PInvoke.GetProcessDefaultCpuSets(hproc, null, 0, &cpuidsCount);
            ushort cpumasksCount = 0;
            PInvoke.GetProcessDefaultCpuSetMasks(hproc, null, 0, &cpumasksCount);
            PInvoke.CloseHandle(hproc);

            return new()
            {
                PID = _PID,
                Priority = priority,
                Mask = mask,
                CpuSetsCount = cpuidsCount,
                CpuSetMasksCount = cpumasksCount,
            };
        }
    }

    public List<ThreadDetailedInfo> GetThreadDetailedInfos()
    {
        var tdis = new List<ThreadDetailedInfo>();
        foreach (var tid in GetTIDs())
        {
            unsafe
            {
                var hth = PInvoke.OpenThread((THREAD_ACCESS_RIGHTS)OpenThreadAccess, false, tid);
                if (hth.IsNull)
                    continue;
                
                PWSTR pstrDesc;
                PInvoke.GetThreadDescription(hth, &pstrDesc);
                var priority = PInvoke.GetThreadPriority(hth);
                PROCESSOR_NUMBER pn = new();
                PInvoke.GetThreadIdealProcessorEx(hth, &pn);
                GROUP_AFFINITY ga = new();
                PInvoke.GetThreadGroupAffinity(hth, &ga);
                uint cpuid = 0, cpuidsCount = 0;
                PInvoke.GetThreadSelectedCpuSets(hth, &cpuid, 1, &cpuidsCount);
                ushort cpumasksCount = 0;
                PInvoke.GetThreadSelectedCpuSetMasks(hth, null, 0, &cpumasksCount);
                PInvoke.CloseHandle(hth);

                tdis.Add(new ThreadDetailedInfo()
                {
                    Name = pstrDesc.ToString(),
                    TID = tid,
                    Priority = priority,
                    Mask = ga.Mask,
                    CpuSetID = cpuid,
                    CpuSetsCount = cpuidsCount,
                    CpuSetMasksCount = cpumasksCount,
                    Ideal = pn.Number,
                });
            }
        }
        return tdis;
    }

    public bool? ToggleScheduling(bool isRedistribution)
    {
        var tdi = GetThreadDetailedInfos();
        
        foreach (var etn in EmphasisThreadNames)
        {
            foreach (var et in tdi.Where(x => etn.IsMatch(x.Name)))
            {
                
            }
        }

        return null;
    }

    private readonly uint _PID = 0;
    private readonly uint _MainTID = 0;
}
