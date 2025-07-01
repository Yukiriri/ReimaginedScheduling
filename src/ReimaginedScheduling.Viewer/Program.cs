using Windows.System;
using ReimaginedScheduling.Lib;
using ReimaginedScheduling.Lib.Tool;
using ReimaginedScheduling.Lib.Windows.Info;

ProcessRequire.setLastCpu();
ProcessRequire.enableSeDebug();

var is_paused = false;
var is_sort_cycletime = true;
var is_hide_nameless = true;

for (;; Thread.Sleep(1))
{
    Console.Clear();
    Console.Write("Ctrl + Ins \r");
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
                case 's': is_sort_cycletime = !is_sort_cycletime; break;
                case 'h': is_hide_nameless = !is_hide_nameless; break;
            }
        }
        if (++update_ticks < 10) continue;
        if (is_paused) continue;
        update_ticks = 0;
        Console.WriteLine("'q': Quit           | 退出");
        Console.WriteLine("'p': Pause          | 暂停");
        Console.WriteLine("'s': Sort CycleTime | 降序CycleTime");
        Console.WriteLine("'h': Hide nameless  | 隐藏无名");

        if (w_info.isInvalid)
        {
            break;
        }
        try
        {
            var p_cpu_info = new ProcessCpuInfo(pid);
            var t_cpu_infos = ProcessBaseInfo.listThreadIds(pid)
                .Select(x => new ThreadCpuInfo(x))
                .Where(x => x.isValid);
            if (is_hide_nameless)
                t_cpu_infos = t_cpu_infos.Where(x => x.id == main_tid || x.currentName.Length > 0);
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
                                   p_cpu_info.id,
                                   new string([..p_cpu_info.exeName.Take(40)]),
                                   p_cpu_info.currentPriority,
                                   $"{p_cpu_info.currentCpuMask:X}",
                                   $"{p_cpu_info.currentCpuSets.FirstOrDefault()}({p_cpu_info.currentCpuSetCount})",
                                   "",
                                   $"{p_cpu_info.currentCycleTime:N0}")
                               + tb_header_split
                               + string.Join("", t_cpu_infos.Select(x => string.Format(tb_fmt_data,
                                   x.id,
                                   new string([..x.currentName.Take(40)]),
                                   x.currentPriority,
                                   $"{x.currentCpuMask:X}",
                                   $"{x.currentCpuSets.FirstOrDefault()}({x.currentCpuSetCount})",
                                   x.currentCpuIdealNumber,
                                   $"{x.currentCycleTime:N0}")))
                               + tb_header_split;
            Console.WriteLine(tb_final_str);
            MyConsole.fillSpace();
            MyConsole.scrollToTop();
        }
        catch (Exception e)
        {
            MyLogger.info(e.ToString());
            Thread.Sleep(1000);
        }
    }
}
