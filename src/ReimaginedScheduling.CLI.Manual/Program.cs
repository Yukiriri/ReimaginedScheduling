using ReimaginedScheduling.Common;
using ReimaginedScheduling.Common.Process;
using ReimaginedScheduling.Common.Process.Tool;
using ReimaginedScheduling.Common.Windows.Device;
using ReimaginedScheduling.Common.Windows.Info;
using System;
using System.Linq;
using System.Threading;
using Windows.System;
using Windows.Win32;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCPU();

while (true)
{
    Console.Write("Ctrl + PageUp/PageDown\r");
    
    var pid = 0u;
    var maintid = 0u;
    // var windowName = "";
    var isRenew = true;
    for (; pid == 0; Thread.Sleep(1))
    {
        if (HotKey.IsKeyDown(VirtualKey.Control) && (HotKey.IsKeyDown(VirtualKey.PageUp) || HotKey.IsKeyDown(VirtualKey.PageDown)))
        {
            isRenew = HotKey.IsKeyDown(VirtualKey.PageUp);
            var wi = new WindowInfo(PInvoke.GetForegroundWindow());
            pid = wi.CurrentPID;
            maintid = wi.CurrentTID;
            // windowName = wi.GetDisplayName(40);
        }
    }

    var threadInfos = ProcessInfo.GetTIDs(pid)
        .Select(x => new ThreadInfo(x))
        .Where(x => x.IsValid && x.CurrentName.Length > 0)
        .Select(x => (x.TID, x.CurrentName));
    var distribution = TDistributionBuilder.Build([..threadInfos], maintid);
    MyLogger.Info(distribution.ToLog());
    distribution.ApplyToProcess(pid, isRenew);
    Thread.Sleep(2000);
}
