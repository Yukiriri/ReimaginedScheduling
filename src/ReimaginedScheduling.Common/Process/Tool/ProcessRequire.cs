using ReimaginedScheduling.Common.Windows.Info;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Security;

namespace ReimaginedScheduling.Common.Process.Tool;

public class ProcessRequire
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

    public static bool SetLastCPU()
    {
        return PInvoke.SetProcessDefaultCpuSets(PInvoke.GetCurrentProcess_SafeHandle(), [CPUSetInfo.PhysicalPECores.Last()]);
    }
}
