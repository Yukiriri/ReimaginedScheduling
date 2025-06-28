using System.Diagnostics;
using Windows.System;
using ReimaginedScheduling.Lib;
using ReimaginedScheduling.Lib.Tool;
using ReimaginedScheduling.Lib.Windows.Info;

ProcessRequire.setLastCpu();
ProcessRequire.enableSeDebug();

var is_paused = false;
var is_culling_row = true;
var is_sort_cycletime = true;
var is_hide_nameless = true;

for (;; Thread.Sleep(1))
{
    Console.Clear();
    Console.Write(" Ctrl + Ins \r");
    MyHotkey.waitDown(VirtualKey.Control, VirtualKey.Insert);

    var w_info = WindowInfo.getMousePointWindow();
    var pid = w_info.ownerProcessId;
    var main_tid = w_info.ownerThreadId;

    for (var update_ticks = 0; pid != 0; Thread.Sleep(100))
    {
        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey().KeyChar)
            {
                case 'q': pid = 0; break;
                case 'p': is_paused = !is_paused; break;
                case 'c': is_culling_row = !is_culling_row; break;
                case 's': is_sort_cycletime = !is_sort_cycletime; break;
                case 'h': is_hide_nameless = !is_hide_nameless; break;
            }
        }
        if (++update_ticks < 10) continue;
        if (is_paused) continue;
        update_ticks = 0;
        Console.WriteLine("'q': Quit           | 退出 \r");
        Console.WriteLine("'p': Pause          | 暂停");
        Console.WriteLine("'c': Culling row    | 剔除显示");
        Console.WriteLine("'s': Sort CycleTime | 降序CycleTime");
        Console.WriteLine("'h': Hide nameless  | 隐藏无名");

        var process = Process.GetProcessById(pid);
        if (process.HasExited)
        {
            break;
        }
        var p_cpu_info = new ProcessCpuInfo(pid);
        var t_cpu_infos = ProcessInfo.listThreadIds(pid).Select(x => new ThreadCpuInfo(x)).Where(x => x.isValid);
        if (is_hide_nameless)
            t_cpu_infos = t_cpu_infos.Where(x => x.threadId == main_tid || x.currentName.Length > 0);
        if (is_sort_cycletime)
            t_cpu_infos = t_cpu_infos.OrderByDescending(x => x.currentCycleTime);

        var tb_header =  $"|ID    |{"Name",-40}|Priority|{"Mask",-16}|CpuSets|Ideal |{"CycleTime",24}|\n";
        var tb_fmt_data = "|{0,-6}|{1,-40     }|{2,-8  }|{3,-16     }|{4,-7 }|{5,-6}|{6,24          }|\n";
        var tb_header_split = new string('-', tb_header.Length - 1) + "\n";
        var tb_final_str = $"\n{w_info.currentName}   \n"
                           + tb_header_split
                           + tb_header
                           + tb_header_split
                           + string.Format(tb_fmt_data,
                               process.Id,
                               new string([..process.ProcessName.Take(40)]),
                               process.PriorityClass,
                               $"{p_cpu_info.currentCpuMask:X}",
                               $"{p_cpu_info.currentCpuSets.FirstOrDefault()}({p_cpu_info.currentCpuSetCount})",
                               "",
                               $"{p_cpu_info.currentCycleTime:N0}")
                           + tb_header_split
                           + string.Join("", t_cpu_infos.Select(x => string.Format(tb_fmt_data,
                               x.threadId,
                               new string([..x.currentName.Take(40)]),
                               x.currentPriority,
                               $"{x.currentCpuMask:X}",
                               $"{x.currentCpuSets.FirstOrDefault()}({x.currentCpuSetCount})",
                               x.currentCpuIdealNumber,
                               $"{x.currentCycleTime:N0}")))
                           + tb_header_split;
        foreach (var s in tb_final_str.Split('\n'))
        {
            if (is_culling_row && Console.CursorTop >= Console.WindowHeight - 1)
                break;
            Console.WriteLine(s);
        }
        MyConsole.fillSpace();
        MyConsole.scrollToTop();
    }
}
