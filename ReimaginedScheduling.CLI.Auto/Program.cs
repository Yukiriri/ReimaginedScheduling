using ReimaginedScheduling.CLI.Auto;
using ReimaginedScheduling.Shared;
using System.Threading;

ProcessUtilities.EnableSeDebug();
ProcessUtilities.SetLastCPU();

for (var gpm = new GameProcessManager(); ; Thread.Sleep(1000))
{
    if (gpm.IsGameProcess())
    {
        gpm.TryAttachGameProcess();
    }
    gpm.Update();
}
