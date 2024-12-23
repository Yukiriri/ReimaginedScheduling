using ReimaginedScheduling.Shared;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

ProcessUtilities.EnableSeDebug();
ProcessUtilities.SetLastCPU();

if (args.Length >= 1)
{
    var si = new ProcessStartInfo()
    {
        FileName = args[0],
        WorkingDirectory = new Regex(@".*(?=\\)").Match(args[0]).Value,
    };
    if (args.Length >= 2)
    {
        si.Arguments = string.Join(" ", args[1..]);
    }
    var proc = new Process()
    {
        StartInfo = si
    };
    if (proc.Start())
    {
        // var pid = 0u;
        // var maintid = 0u;
        // var windowName = "";

        // var distribution = DistributionGenerator.Generate(pid, maintid);
        // DistributionGenerator.ToggleScheduling(pid, windowName, distribution, true);
    }
}
