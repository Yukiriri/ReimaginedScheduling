using System.Diagnostics;
using ReimaginedScheduling.Lib;
using ReimaginedScheduling.Lib.Tool;
using ReimaginedScheduling.Lib.Windows.Info;

ProcessRequire.setLastCpu();
ProcessRequire.enableSeDebug();

var rotation_interval = 1000 / CpuSetInfo.PCores.Count;
var exe_list = new List<string>();

for (;;Thread.Sleep(1))
{
    Console.Clear();
    if (MyHotSaved.isChanged())
    {
        exe_list = [..MyHotSaved.reload().Split('\n').Select(x => x.TrimEnd('\r', '\n'))];
    }

    var process_list = new List<Process>();
    process_list.AddRange(Process.GetProcesses().Where(x => exe_list.Contains(x.ProcessName + ".exe")));
    process_list.ForEach(x => Console.WriteLine(x.ProcessName + ".exe"));
    
    var tid_list = new List<int>();
    tid_list.AddRange(process_list.SelectMany(process => ProcessInfo.listThreadIds(process.Id)));
    Console.WriteLine($"all thread count: {tid_list.Count}");

    for (var core_index = 0; core_index < CpuSetInfo.PCores.Count; core_index++)
    {
        foreach (var tid in tid_list)
        {
            new ThreadCpuInfo(tid).currentCpuIdealNumber = core_index;
        }
        Console.Write($"core index: {core_index}   \r");
        Thread.Sleep(rotation_interval);
    }
}
