using ReimaginedScheduling.Common;
using ReimaginedScheduling.Common.Model;
using ReimaginedScheduling.Common.Tool;
using ReimaginedScheduling.Common.Windows.Device;
using ReimaginedScheduling.Common.Windows.Info;
using ReimaginedScheduling.Common.Windows.Info.Window;
using System;
using System.Linq;
using System.Threading;
using Windows.System;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCPU();

while (true)
{
    Console.Write(" Ctrl + PageUp/PageDown\r");
    for (; !(HotKey.IsKeyDown(VirtualKey.Control) && (HotKey.IsKeyDown(VirtualKey.PageUp) || HotKey.IsKeyDown(VirtualKey.PageDown))); Thread.Sleep(1));
    var isOn = HotKey.IsKeyDown(VirtualKey.PageUp);
    var wi = new MousePointWindowInfo();
    var pid = wi.CurrentPID;
    var maintid = wi.CurrentTID;
    Console.Write("                       \r");
    #if !DEBUG
    GC.Collect();
    #endif

    var threadInfos = ProcessInfo.GetTIDs(pid)
        .Select(x => new ThreadInfo(x))
        .Where(x => x.IsValid && x.CurrentName.Length > 0)
        .Select(x => (x.TID, x.CurrentName));
    var distribution = TDistributionBuilder.Build([..threadInfos], maintid);
    MyLogger.Info(distribution.ToLog());
    distribution.ApplyToProcess(pid, isOn);
    Thread.Sleep(2000);
}
