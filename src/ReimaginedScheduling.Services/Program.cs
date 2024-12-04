using ReimaginedScheduling.Services;
using ReimaginedScheduling.Services.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vanara.PInvoke;

string[] description = ["未知", "未知", "未知", "未知"];
if (CPUSetInfo.PCoreEfficiencyIndex >= 2) description[CPUSetInfo.PCoreEfficiencyIndex - 2] = "LPE核";
if (CPUSetInfo.PCoreEfficiencyIndex >= 1) description[CPUSetInfo.PCoreEfficiencyIndex - 1] = "E核";
if (CPUSetInfo.PCoreEfficiencyIndex >= 0) description[CPUSetInfo.PCoreEfficiencyIndex - 0] = "P核";
CPUSetInfo.CoreSetList.ForEach(x =>
{
   var cs = x.CpuSet;
   MyLogger.Debug($"ID:{cs.Id,-3} 物理核心:{cs.CoreIndex,-3} 逻辑核心:{cs.LogicalProcessorIndex,-3} 异构类型:{description[cs.EfficiencyClass],-5}");
});
MyLogger.Debug($"CPU异构组合: {CPUSetInfo.PhysicalPCoreList.Count}P + {CPUSetInfo.ECoreList.Count}E");
MyLogger.Debug(Config.ConsoleSplitRow);

// for (var pm = new PerformanceMonitor(); ; Thread.Sleep(501))
// {
//    pm.Update();
// //    Console.Clear();
//    Console.WriteLine($"CPU: {pm.GetAllCPUUsage():F2}%");
//    Console.WriteLine($"GPU: {pm.GetAllGPUUsage():F2}%");
//    Console.WriteLine($"GPUMem: {pm.GetAllGPUMemUsage() >> 20}MB");
// //    Console.WriteLine($"dwm GPU: {pm.GetGPUUsage(1844):F2}%");
// //    Console.WriteLine($"dwm GPUMem: {pm.GetGPUMemUsage(1844) >> 20}MB");
//    pm.GetProcessThreadIDWithUsage("dwm", 10).ToList().ForEach(x => Console.WriteLine($"{x.ThreadID} {x.Usage:F2}"));
//    Console.WriteLine();
// }

MyLogger.Debug($"SeDebug: {GameRules.SeDebug()}");
MyLogger.Debug(Config.ConsoleSplitRow);

// GameThreadManager gtm = new();
// List<PerformanceMonitor.ProcessThreadIDWithUsage[]> data =
//     [
//         [new(0  ,10320,41), new(4  ,3724 ,11), new(5  ,13416,44), new(10 ,3392 ,11), new(12 ,11380,12), new(33 ,11704,12), new(34 ,4844 ,13), new(35 ,1396 ,13), new(36 ,16868,12), new(38 ,17388,13), new(39 ,16420,12), new(40 ,15556,15), new(94 ,8904 ,11), new(97 ,13424,11), new(98 ,1264 ,11), new(99 ,13868,11), new(100,3796 ,11), new(103,15372,11), new(104,16936,11), new(105,12472,11), new(106,5536 ,11), new(107,8072 ,11), ],
//     ];

// data.ForEach(x =>
// {
//     if (gtm.Update(x))
//         MyLogger.Info(gtm.ToString());
//     Thread.Sleep(100);
// });

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
