using ReimaginedScheduling.Shared;
using System;
using System.Threading;
using Windows.System;
using Windows.Win32;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCpu();

while (true)
{
    Console.Write("Ctrl + PageUp/PageDown\r");
    
    var pid = 0u;
    var maintid = 0u;
    var windowName = "";
    for (; pid == 0; Thread.Sleep(1))
    {
        if (HotKey.IsCtrl && (HotKey.IsKeyDown(VirtualKey.PageUp) || HotKey.IsKeyDown(VirtualKey.PageDown)))
        {
            var wi = new WindowInfo(PInvoke.GetForegroundWindow());
            pid = wi.GetPID();
            windowName = wi.GetDisplayName(40);
            maintid = wi.GetTID();
        }
    }

    var distribution = DistributionGenerator.Generate(pid, maintid);
    DistributionGenerator.ToggleScheduling(pid, windowName, distribution, HotKey.IsKeyDown(VirtualKey.PageUp));
    Thread.Sleep(2000);
}
