using System.Collections.Generic;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.System.Diagnostics.ToolHelp;

namespace ReimaginedScheduling.Shared;

public class ProcessUtilities
{
    public static unsafe bool EnableSeDebug()
    {
        if (!PInvoke.OpenProcessToken(PInvoke.GetCurrentProcess_SafeHandle(), TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, out var hToken))
            return false;
        if (!PInvoke.LookupPrivilegeValue(null, "SeDebugPrivilege", out var luid))
            return false;
        var tp = new TOKEN_PRIVILEGES()
        {
            PrivilegeCount = 1,
            Privileges = new()
            {
                e0 = new()
                {
                    Luid = luid,
                    Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
                }
            }
        };
        return PInvoke.AdjustTokenPrivileges(hToken, false, &tp, (uint)sizeof(TOKEN_PRIVILEGES), null, null);
    }

    public static unsafe void SetLastCPU()
    {
        uint cpuid = CPUSetInfo.PhysicalPECores.Last();
        PInvoke.SetProcessDefaultCpuSets(PInvoke.GetCurrentProcess(), &cpuid, 1);
    }

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
}
