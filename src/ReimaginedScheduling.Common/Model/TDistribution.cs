using ReimaginedScheduling.Common.Windows.Info;
using System.Collections.Generic;
using System.Linq;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Common.Model;

public class TDistribution
{
    public struct Attribution(uint TID, string Name, uint[] CPUIDs, uint Ideal)
    {
        public uint TID = TID;
        public string Name = Name;
        public uint[] CPUIDs = CPUIDs;
        public uint Ideal = Ideal;
    }
    public List<Attribution> Attributions = [];
    public List<uint> SharedCPUIDs = [];

    public bool ApplyToProcess(uint PID, bool isOn)
    {
        var pi = new ProcessInfo(PID);
        if (pi.IsInvalid)
            return false;

        Attributions.ForEach(attribution =>
        {
            var ti = new ThreadInfo(attribution.TID);
            if (ti.IsValid)
            {
                if (isOn)
                {
                    ti.CurrentCpuSets = attribution.CPUIDs;
                    ti.CurrentIdealNumber = attribution.Ideal;
                }
                else
                {
                    ti.CurrentCpuSets = [];
                }
            }
        });
        pi.CurrentPriority = isOn ? (uint)PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS : (uint)PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS;
        pi.CurrentCpuSets = isOn ? [..SharedCPUIDs] : [];
        return true;
    }

    public string ToLog()
    {
        var headerstr =    $"|TID   |{"Name",-40}|Ideal |{"CpuSets",-40}|";
        var dataformatstr = "|{0,-6}|{1,-40     }|{2,-6}|{3,-40        }|\n";
        var headersplitstr = new string('-', headerstr.Length);

        var datastr = "";
        Attributions.ForEach(attribution =>
        {
            datastr += string.Format(dataformatstr,
                attribution.TID,
                new string([..attribution.Name.Take(40)]),
                attribution.Ideal,
                string.Join(',', attribution.CPUIDs.Select(x => x - CPUSetInfo.BeginCPUID)));
        });
            datastr += string.Format(dataformatstr,
                "...",
                "...",
                "...",
                string.Join(',', SharedCPUIDs.Select(x => x - CPUSetInfo.BeginCPUID)));

        return "\n" +
            headersplitstr + '\n' +
            headerstr + '\n' +
            headersplitstr + '\n' +
            datastr +
            headersplitstr + '\n';
    }
}
