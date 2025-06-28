using ReimaginedScheduling.Lib.Tool;
using ReimaginedScheduling.Pattern;

ProcessRequire.enableSeDebug();
ProcessRequire.setLastCpu();

for (var gpm = new GameProcessManager(); ; Thread.Sleep(1000))
{
    if (gpm.isGameProcess())
    {
        gpm.tryAddGameProcess();
    }
    gpm.update();
}
