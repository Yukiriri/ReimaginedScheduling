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
    Console.Clear();
    if (MyHotSaved.isChanged())
    {
        exe_list = [..MyHotSaved.reload().Split('\n').Select(x => x.TrimEnd('\r', '\n'))];
    }

    var ms = currentMs();
    var matched_processes = new List<Process>(Process.GetProcesses()
        .Where(x => exe_list.Contains($"{x.ProcessName}.exe")));
    matched_processes.ForEach(x => Console.WriteLine($"* {x.ProcessName}.exe"));
    Console.WriteLine();

    var t_cpu_infos = new List<ThreadCpuInfo>(matched_processes
        .SelectMany(process => ProcessInfo.listThreadIds(process.Id))
        .Select(x => new ThreadCpuInfo(x)));
    Console.WriteLine($"all thread count: {t_cpu_infos.Count,-4} load: {currentMs() - ms}ms   ");

    for (var core_index = 0; core_index < CpuSetInfo.PCores.Count; core_index++)
    {
        t_cpu_infos.ForEach(x =>
        {
            x.currentCpuIdealNumber = core_index;
        });
        Console.Write($"core index: {core_index,-3}\r");
        Thread.Sleep(rotation_interval);
    }
}
