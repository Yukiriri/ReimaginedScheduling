using ReimaginedScheduling.Services;
using ReimaginedScheduling.Services.Utils;
using System;
using System.Linq;
using System.Threading;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Kernel;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;

bool isHideNameless = true;
for (;;Thread.Sleep(1))
{
    Console.Write("Ctrl + Ins \r");

    string windowName = "";
    uint pid = 0;
    if ((PInvoke.GetAsyncKeyState((int)VirtualKey.Insert) & 0x8000) != 0 &&
        (PInvoke.GetAsyncKeyState((int)VirtualKey.Control) & 0x8000) != 0)
    {
        unsafe
        {
            var hwnd = PInvoke.GetForegroundWindow();
            var textLength = PInvoke.GetWindowTextLength(hwnd) + 1;
            var text = new char[textLength];
            fixed (char* textPtr = text)
            {
                if (PInvoke.GetWindowText(hwnd, textPtr, textLength) > 0)
                    windowName = new string(text);
            }
            PInvoke.GetWindowThreadProcessId(hwnd, &pid);
        }
    }

    for (; pid != 0; Thread.Sleep(1000))
    {
        // Console.Clear();
        Console.SetCursorPosition(0, 0);
        unsafe
        {
            {
                var hproc = PInvoke.OpenProcess((PROCESS_ACCESS_RIGHTS)GameThreadManager.OpenProcessAccess, false, pid);
                if (hproc.IsNull)
                    break;
                var priority = PInvoke.GetPriorityClass(hproc);
                nuint mask = 0, mask2;
                PInvoke.GetProcessAffinityMask(hproc, &mask, &mask2);
                uint cpuidsCount = 0;
                PInvoke.GetProcessDefaultCpuSets(hproc, null, 0, &cpuidsCount);
                ushort cpumasksCount = 0;
                PInvoke.GetProcessDefaultCpuSetMasks(hproc, null, 0, &cpumasksCount);
                PInvoke.CloseHandle(hproc);

                var str = $"|{"Name",-40}|PID  |Priority|{"Mask",-16}|CpuSets|CpuSetMasks|";
                Console.WriteLine(str);
                Console.WriteLine(new string('-', str.Length));
                var showlength = windowName.Aggregate(0, (length, next) => length + (next > 127 ? 2 : 1));
                if (showlength > 40)
                {
                    windowName = windowName[0..20];
                }
                windowName += new string(' ', 40 - showlength);
                str = $"|{windowName}";
                str += $"|{pid,-5}";
                str += $"|{priority,-8}";
                str += $"|{mask:X16}";
                str += $"|{$"({cpuidsCount})",-7}";
                str += $"|{$"({cpumasksCount})",-11}";
                str += "|";
                Console.WriteLine(str);
                Console.Write(Config.ConsoleSplitRow);
                MyConsole.FillLine("");
                MyConsole.FillLine("");
            }
            {
                var str = $"|{"Name",-40}|TID  |Priority|{"Mask",-16}|CpuSets|CpuSetMasks|Ideal|";
                MyConsole.FillLine(str);
                MyConsole.FillLine(new string('-', str.Length));

                var gtm = new GameThreadManager(pid);
                var tids = gtm.GetTIDs();
                for (int i = 0; i < tids.Length; i++)
                {
                    var tid = tids[i];
                    var hth = PInvoke.OpenThread((THREAD_ACCESS_RIGHTS)GameThreadManager.OpenThreadAccess, false, tid);
                    if (hth.IsNull)
                        continue;
                    PWSTR pstrDesc;
                    PInvoke.GetThreadDescription(hth, &pstrDesc);
                    string threadName = pstrDesc.ToString();
                    var priority = PInvoke.GetThreadPriority(hth);
                    PROCESSOR_NUMBER pn = new();
                    PInvoke.GetThreadIdealProcessorEx(hth, &pn);
                    GROUP_AFFINITY ga = new();
                    PInvoke.GetThreadGroupAffinity(hth, &ga);
                    uint cpuid = 0, cpuidsCount = 0;
                    PInvoke.GetThreadSelectedCpuSets(hth, &cpuid, 1, &cpuidsCount);
                    ushort cpumasksCount = 0;
                    PInvoke.GetThreadSelectedCpuSetMasks(hth, null, 0, &cpumasksCount);
                    PInvoke.CloseHandle(hth);
                    
                    if (isHideNameless && i > 0 && threadName.Length == 0)
                        continue;
                    str = $"|{threadName[0..Math.Min(threadName.Length, 40)],-40}";
                    str += $"|{tid,-5}";
                    str += $"|{priority,-8}";
                    str += $"|{ga.Mask:X16}";
                    str += $"|{$"{cpuid}({cpuidsCount})",-7}";
                    str += $"|{$"({cpumasksCount})",-11}";
                    str += $"|{pn.Number,-5}";
                    str += "|";
                    MyConsole.FillLine(str);
                }
            }
        }
        Console.Write(Config.ConsoleSplitRow);
        MyConsole.FillLine("'Q': quit");
        MyConsole.FillLine("'N': toggle nameless");
        MyConsole.FillConsole();

        Console.SetWindowPosition(0, 0);
        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey().KeyChar)
            {
                case 'q':
                    pid = 0;
                    Console.Clear();
                    break;
                case 'n':
                    isHideNameless = !isHideNameless;
                    break;
            }
        }
    }
}
