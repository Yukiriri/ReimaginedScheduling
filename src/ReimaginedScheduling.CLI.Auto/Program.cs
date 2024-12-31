using ReimaginedScheduling.Auto;
using ReimaginedScheduling.Common.Tool;
using System.Threading;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCPU();

for (var gpm = new GameProcessManager(); ; Thread.Sleep(1000))
{
    if (gpm.IsGameProcess())
    {
        gpm.TryAttachGameProcess();
    }
    gpm.Update();
}
