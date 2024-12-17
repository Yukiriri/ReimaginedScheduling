using ReimaginedScheduling.CLI.Auto;
using ReimaginedScheduling.Shared;
using System.Threading;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCpu();

for (var gpm = new GameProcessManager(); ; Thread.Sleep(1000))
{
    if (gpm.IsGameProcess())
    {
        gpm.TryAttachGameProcess();
    }
    gpm.Update();
}
