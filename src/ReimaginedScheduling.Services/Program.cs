using ReimaginedScheduling.Services;
using ReimaginedScheduling.Services.Utils;
using System;
using System.Linq;
using System.Threading;
using Vanara.PInvoke;

//for (var pm = new PerformanceMonitor(); ; Thread.Sleep(501))
//{
//    pm.Update();
//    Console.Clear();
//    Console.WriteLine($"CPU: {pm.GetAllCPUUsage():F2}%");
//    Console.WriteLine($"GPU: {pm.GetAllGPUUsage():F2}%");
//    Console.WriteLine($"GPUMem: {pm.GetAllGPUMemUsage() >> 20}MB");
//    Console.WriteLine($"dwm GPU: {pm.GetGPUUsage(1844):F2}%");
//    Console.WriteLine($"dwm GPUMem: {pm.GetGPUMemUsage(1844) >> 20}MB");
//    pm.GetProcessThreadIDWithUsage("dwm", 20).ToList().ForEach(x => Console.WriteLine($"{x.threadID} {x.usage:F2}"));
//    Console.WriteLine();
//}

string[] description = ["未知", "未知", "未知", "未知"];
if (CPUSetInfo.PCoreIndex >= 2) description[CPUSetInfo.PCoreIndex - 2] = "LPE核";
if (CPUSetInfo.PCoreIndex >= 1) description[CPUSetInfo.PCoreIndex - 1] = "E核";
if (CPUSetInfo.PCoreIndex >= 0) description[CPUSetInfo.PCoreIndex - 0] = "P核";
CPUSetInfo.CoreSetList.ForEach(x =>
{
    var cs = x.CpuSet;
    Console.WriteLine($"ID:{cs.Id,-3} 物理核心:{cs.CoreIndex,-3} 逻辑核心:{cs.LogicalProcessorIndex,-3} 异构类型:{description[cs.EfficiencyClass],-5}");
});
Console.WriteLine($"CPU异构组合：{CPUSetInfo.PhysicalPCoreList.Count}P + {CPUSetInfo.ECoreList.Count}E");
Console.WriteLine($"P核：[{CPUSetInfo.PCoreList.First()} - {CPUSetInfo.PCoreList.Last()}]");
Console.WriteLine($"物理P核：{string.Join(" ", CPUSetInfo.PhysicalPCoreList)}");
if (!CPUSetInfo.IsPCoreOnly)
{
    Console.WriteLine($"E核：[{CPUSetInfo.ECoreList.First()} - {CPUSetInfo.ECoreList.Last()}]");
}
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
