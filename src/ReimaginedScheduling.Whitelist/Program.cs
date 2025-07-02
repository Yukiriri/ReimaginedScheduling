using System.Diagnostics;
using ReimaginedScheduling.Lib;
using ReimaginedScheduling.Lib.Tool;
using ReimaginedScheduling.Lib.Windows.Info;

ProcessRequire.setLastCpu();
ProcessRequire.enableSeDebug();

long currentMs() => DateTimeOffset.Now.ToUnixTimeMilliseconds();
var rotation_interval = 1000 / CpuSetInfo.PCores.Count;
var exe_list = new List<string>();

for (;;Thread.Sleep(1))
{
    if (MyHotSaved.isChanged())
    {
        exe_list = [..MyHotSaved.reload().Split('\n').Select(x => x.TrimEnd('\r', '\n'))];
    }

    try
    {
        Console.Clear();
        var ms = currentMs();
        
        var matched_processes = new List<ProcessBaseInfo>(Process.GetProcesses()
            .Where(x => exe_list.Contains($"{x.ProcessName}.exe"))
            .Select(x => new ProcessBaseInfo(x.Id))
            .Where(x => x.isValid));
        matched_processes.ForEach(x =>
        {
            if (x.currentPriority < (uint)ProcessPriorityClass.High)
                x.currentPriority = (uint)ProcessPriorityClass.High;
            Console.WriteLine($"* {x.exeName}");
        });
        Console.WriteLine();
        
        var t_cpu_infos = new List<ThreadCpuInfo>(matched_processes
            .SelectMany(x => ProcessBaseInfo.listThreadIds(x.id))
            .Select(x => new ThreadCpuInfo(x))
            .Where(x => x.isValid));
        Console.WriteLine($"load: {currentMs() - ms}ms / thread count: {t_cpu_infos.Count,-4}   ");

        for (var core_index = 0; core_index < CpuSetInfo.PCores.Count; core_index++)
        {
            t_cpu_infos.ForEach(x =>
            {
                x.currentCpuIdealNumber = core_index;
            });
            Console.Write($"interval: {rotation_interval}ms / core index: {core_index,-3}   \r");
            Thread.Sleep(rotation_interval);
        }
    }
    catch (Exception e)
    {
        MyLogger.info(e.ToString());
        Thread.Sleep(1000);
    }
}
