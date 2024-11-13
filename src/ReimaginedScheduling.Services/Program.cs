using ReimaginedScheduling.Services;
using ReimaginedScheduling.Services.Utils;
using Vanara.PInvoke;

CPUSetInfo.WriteLine();
Console.WriteLine(Config.ConsoleSplitRow);


//var pm = new PerformanceMonitor();
//for (int i = 0; i < 10; i++)
//{
//    Console.Clear();

//    Thread.Sleep(501);
//}


Console.WriteLine($"SeDebug: {GameRules.SeDebug()}");
Console.WriteLine(Config.ConsoleSplitRow);
for (var gr = new GameRules(); ; Thread.Sleep(1000))
{
    var hwnd = User32.GetForegroundWindow();
    if (hwnd.IsNull)
        continue;

    if (gr.IsGameProcess(hwnd))
    {
        gr.AddGameProcess(hwnd);
    }
    gr.UpdateSampling();
}
