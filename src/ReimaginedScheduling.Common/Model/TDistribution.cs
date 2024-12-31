using ReimaginedScheduling.Common.Windows.Info;
using System.Collections.Generic;
using System.Linq;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Common.Model;

public class TDistribution
{
    public struct Attribution(uint TID, string Name, uint[] CPUIDs)
    {
        public uint TID = TID;
        public string Name = Name;
        public uint[] CPUIDs = CPUIDs;
    }
    public List<Attribution> Attributions = [];
    public List<uint> SharedCPUIDs = [];

    public string ToLog()
    {
        var headerstr = $"|   ID|{"Name",-40}|{"CPU",-40}|";
        var headersplitstr = new string('-', headerstr.Length);

        var thstr = "";
        foreach (var attribution in Attributions)
        {
            thstr += $"|{attribution.TID,5}";
            thstr += $"|{new string([..attribution.Name.Take(40)]),-40}";
            thstr += $"|{string.Join(',', attribution.CPUIDs.Select(x => x - CPUSetInfo.BeginCPUID)),-40}";
            thstr += "|\n";
        }
        thstr += $"|{"...",5}";
        thstr += $"|{"...",-40}";
        thstr += $"|{string.Join(',', SharedCPUIDs.Select(x => x - CPUSetInfo.BeginCPUID)),-40}";
        thstr += "|\n";

        var str = "\n" +
            headersplitstr + '\n' +
            headerstr + '\n' +
            headersplitstr + '\n' +
            thstr +
            headersplitstr + '\n';
        return str;
    }

    public bool ApplyToProcess(uint PID, bool isOn)
    {
        var pi = new ProcessInfo(PID);
        if (pi.IsInvalid)
            return false;
        
        foreach (var attribution in Attributions)
        {
            var ti = new ThreadInfo(attribution.TID);
            if (ti.IsValid)
            {
                if (isOn)
                {
                    ti.CurrentIdealNumber = attribution.CPUIDs[0] - CPUSetInfo.BeginCPUID;
                    ti.CurrentCpuSets = [..attribution.CPUIDs];
                }
                else
                {
                    ti.CurrentCpuSets = [];
                }
            }
        }
        if (isOn)
        {
            pi.CurrentPriority = (uint)PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS;
            pi.CurrentCpuSets = [..SharedCPUIDs];
        }
        else
        {
            pi.CurrentPriority = (uint)PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS;
            pi.CurrentCpuSets = [];
        }
        return true;
    }
}
