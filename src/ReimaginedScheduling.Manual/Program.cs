using ReimaginedScheduling.Common;
using ReimaginedScheduling.Common.Model;
using ReimaginedScheduling.Common.Tool;
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
    Console.Write(" Ctrl + PageUp\r");
    MyHotkey.WaitPress(VirtualKey.Control, VirtualKey.PageUp);

    var wi = new MousePointWindowInfo();
    var pid = wi.CurrentPID;
    var maintid = wi.CurrentTID;
    Console.Clear();

    var thinfos = ProcessInfo.GetTIDs(pid)
        .Select(x => new ThreadInfo(x))
        .Where(x => x.IsValid && x.CurrentName.Length > 0)
        .Select(x => (x.TID, x.CurrentName));
    var distribution = TDistributionBuilder.Build([..thinfos], maintid);
    MyLogger.Info(distribution.ToLog());
    distribution.ApplyToProcess(pid, true);
    Thread.Sleep(1200);
}
