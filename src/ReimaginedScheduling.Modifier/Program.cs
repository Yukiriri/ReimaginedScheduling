using ReimaginedScheduling.Common.Tool;
using ReimaginedScheduling.Common.Windows.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCPU();


while (true)
{
    Console.Write(new string('=', Console.WindowWidth));
    Console.Write("PID: "); uint pid = uint.Parse(Console.ReadLine() ?? "");

    var pi = new ProcessInfo(pid);
    var thinfos = new List<(uint, string)>();
    while (pi.IsValid || pi.IsExist())
    {
        Console.Write(">"); var statements = Console.ReadLine() ?? "";
        (Regex command, Action func)[] statementsHandler =
        [
            (new Regex(@"select( cpu=\d)?"), delegate
            {
                thinfos = pi.GetTIDs()
                    .Select(x => new ThreadInfo(x))
                    .Where(x => x.IsValid)
                    .Select(x => (x.TID, x.CurrentName))
                    .ToList();
            }),
            (new Regex(@"where( cpu=\d)?"), delegate
            {

            }),
        ];
        foreach (var handler in statementsHandler)
        {

        }

        // var select = .Match(statements);
        // if (select.Success)
        // {

        // }

        if (statements == "do")
        {

            continue;
        }

        var headerstr = $"|  TID|{"Name",-40}|{"CpuSets",-40}|Ideal|";
        var headersplitstr = new string('-', headerstr.Length);
    }
    Console.WriteLine($"Lost PID {pid} | 没有PID{pid}了");
    pid = 0;
}
