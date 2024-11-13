using ReimaginedScheduling.Services;
using ReimaginedScheduling.Services.Utils;
using System;
using System.Threading;
using Vanara.PInvoke;

CPUSetInfo.WriteLine();
Console.WriteLine(Config.ConsoleSplitRow);

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
