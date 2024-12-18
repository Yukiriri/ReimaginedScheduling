using ReimaginedScheduling.Shared;
using System;
using System.Threading;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCpu();

var pid = 0u;
var windowName = "";
var maintid = 0u;

void ToggleScheduling()
{
    var distribution = DistributionGenerator.Generate(pid, maintid);
    DistributionGenerator.ToggleScheduling(pid, windowName, distribution, HotKey.IsPageUp);
    Thread.Sleep(2000);
    Console.Write(Config.ConsoleSplitRow);
}

if (args.Length == 2)
{
    
}
else
{
    while (true)
    {
        Console.Write("Ctrl + PageUp/PageDown\r");
        
        for (; pid == 0; Thread.Sleep(1))
        {
            if (HotKey.IsCtrl && (HotKey.IsPageUp || HotKey.IsPageDown))
            {
                var wi = new WindowInfo();
                wi.SetForegroundHWND();
                pid = wi.GetPID();
                windowName = wi.GetDisplayName(40);
                maintid = wi.GetTID();
            }
        }

        ToggleScheduling();
        pid = 0;
    }
}
