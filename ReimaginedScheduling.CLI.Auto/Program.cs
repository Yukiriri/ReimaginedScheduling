using ReimaginedScheduling.CLI.Auto;
using ReimaginedScheduling.Shared;
using System;
using System.Linq;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Security;

static unsafe bool EnableSeDebug()
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

// MyLogger.Debug("\n" + CPUSetInfo.ToString());
MyLogger.Debug($"EnableSeDebug: {EnableSeDebug()}");
unsafe
{
    uint cpuid = CPUSetInfo.PhysicalPECores.Last();
    PInvoke.SetProcessDefaultCpuSets(PInvoke.GetCurrentProcess(), &cpuid, 1);
}

for (var gpm = new GameProcessManager(); ; Thread.Sleep(1000))
{
    if (gpm.IsGameProcess())
    {
        gpm.TryAttachGameProcess();
    }
    gpm.Update();
}
