using ReimaginedScheduling.Services;
using ReimaginedScheduling.Services.Utils;
using System;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

static bool EnableSeDebug()
{
    unsafe
    {
        HANDLE hToken;
        if (!PInvoke.OpenProcessToken(PInvoke.GetCurrentProcess(), TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES, &hToken))
            return false;
        if (!PInvoke.LookupPrivilegeValue(null, "SeDebugPrivilege", out var luid))
            return false;
        var laa = new LUID_AND_ATTRIBUTES
        {
            Luid = luid,
            Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED
        };
        var vlia = new VariableLengthInlineArray<LUID_AND_ATTRIBUTES>
        {
            e0 = laa
        };
        var tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges = vlia
        };
        return PInvoke.AdjustTokenPrivileges(hToken, false, &tp, (uint)sizeof(TOKEN_PRIVILEGES));
    }
}

MyLogger.Debug("\n" + CPUSetInfo.ToString());
MyLogger.Debug($"EnableSeDebug: {EnableSeDebug()}");

for (var gpm = new GameProcessManager(); ; Thread.Sleep(1000))
{
    if (gpm.IsGameProcess())
    {
        gpm.AttachGameProcess();
    }
    gpm.Update();
}
