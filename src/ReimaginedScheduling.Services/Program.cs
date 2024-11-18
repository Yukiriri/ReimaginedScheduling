using ReimaginedScheduling.Services;
using ReimaginedScheduling.Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vanara.PInvoke;

//string[] description = ["未知", "未知", "未知", "未知"];
//if (CPUSetInfo.PCoreEfficiencyIndex >= 2) description[CPUSetInfo.PCoreEfficiencyIndex - 2] = "LPE核";
//if (CPUSetInfo.PCoreEfficiencyIndex >= 1) description[CPUSetInfo.PCoreEfficiencyIndex - 1] = "E核";
//if (CPUSetInfo.PCoreEfficiencyIndex >= 0) description[CPUSetInfo.PCoreEfficiencyIndex - 0] = "P核";
//CPUSetInfo.CoreSetList.ForEach(x =>
//{
//    var cs = x.CpuSet;
//    Console.WriteLine($"ID:{cs.Id,-3} 物理核心:{cs.CoreIndex,-3} 逻辑核心:{cs.LogicalProcessorIndex,-3} 异构类型:{description[cs.EfficiencyClass],-5}");
//});
//Console.WriteLine($"CPU异构组合：{CPUSetInfo.PhysicalPCoreList.Count}P + {CPUSetInfo.ECoreList.Count}E");
//Console.WriteLine(Config.ConsoleSplitRow);

//for (var pm = new PerformanceMonitor(); ; Thread.Sleep(501))
//{
//    pm.Update();
//    Console.Clear();
//    Console.WriteLine($"CPU: {pm.GetAllCPUUsage():F2}%");
//    Console.WriteLine($"GPU: {pm.GetAllGPUUsage():F2}%");
//    Console.WriteLine($"GPUMem: {pm.GetAllGPUMemUsage() >> 20}MB");
//    Console.WriteLine($"dwm GPU: {pm.GetGPUUsage(1844):F2}%");
//    Console.WriteLine($"dwm GPUMem: {pm.GetGPUMemUsage(1844) >> 20}MB");
//    pm.GetProcessThreadIDWithUsage("dwm", 20).ToList().ForEach(x => Console.WriteLine($"{x.ThreadID} {x.Usage:F2}"));
//    Console.WriteLine();
//}

//GameThreadManager gtm = new();
//List<PerformanceMonitor.ProcessThreadIDWithUsage[]> data =
//    [
//        [new(00000, 50), new(10000, 20), new(10100, 30), new(10200, 10)],
//        [new(10000, 50), new(00000, 20), new(10200, 30)],
//        [new(10200, 50), new(00000, 20), new(10000, 10)],
//        [new(10200, 50), new(00000, 20), new(10000, 10)],
//        [new(10200, 50), new(00000, 10), new(10000, 20)],
//        [new(10200, 50), new(00000, 20), new(10000, 10)],
//        [new(10000, 50), new(00000, 20)],
//        [new(10000, 50), new(00100, 20)],
//        [new(10000, 50), new(00000, 20), new(00100, 20)],
//    ];
//var rd = new Random();
//for (int i = data.Count; i < 13; i++)
//{
//    var arrsize = 1 + rd.Next(gtm.AvailablePCoreCount);
//    var arr = new PerformanceMonitor.ProcessThreadIDWithUsage[arrsize];
//    for (int j = 0; j < arrsize; j++)
//        arr[j] = new(20000u + (uint)rd.Next(1000), rd.NextDouble() * 100);

//    arr[rd.Next(arrsize)] = data[i - 1][rd.Next(data[i - 1].Length)];

//    data.Add(arr);
//}
//gtm.WriteLineCPUID();
//data.ForEach(x =>
//{
//    gtm.Append(x);
//    if (gtm.IsNeedUpdate)
//        gtm.WriteLineThreadMap();
//});

Console.WriteLine($"SeDebug: {GameRules.SeDebug()}");
Console.WriteLine(Config.ConsoleSplitRow);

for (var gr = new GameRules(); ; Thread.Sleep(1000))
{
    var hwnd = User32.GetForegroundWindow();
    if (hwnd.IsNull)
        continue;

    if (gr.IsGameProcess(hwnd))
    {
        gr.AttachGameProcess(hwnd);
    }
    gr.UpdateSampling();
}
