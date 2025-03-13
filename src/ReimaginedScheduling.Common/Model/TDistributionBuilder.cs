using ReimaginedScheduling.Common.Windows.Info;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReimaginedScheduling.Common.Model;

public class TDistributionBuilder
{
    private struct DistributionModel
    {
        public Regex EnginePattern;
        public List<Regex> ThreadPatterns;
    }
    private static readonly List<DistributionModel> _distributionModels =
    [
        new DistributionModel()
        {
            EnginePattern = new(@"^TaskGraphThread.P \d+"), //UE4
            ThreadPatterns =
            [
                new(@"^RenderThread"),
                new(@"^RHIThread"),
                // new(new(@"^TaskGraphThreadHP \d+")),
            ]
        },
        new DistributionModel()
        {
            EnginePattern = new(@"ground Worker #\d+"), //UE5
            ThreadPatterns =
            [
                new(@"^RenderThread"),
                new(@"^RHIThread"),
                new(@"^RHISubmissionThread"),
                new(@"^Foreground Worker"),
            ]
        },
        new DistributionModel()
        {
            EnginePattern = new(@"^Unity.+"),
            ThreadPatterns =
            [
                new(@"^UnityMultiRenderingThread"),
                new(@"^UnityGfxDeviceWorker"),
            ]
        },
        new DistributionModel()
        {
            EnginePattern = new(@".+"),
            ThreadPatterns =
            [
                new(@"Render.*(T|t)hread"),
            ]
        },
    ];

    public static TDistribution Build(List<(uint TID, string Name)> threadInfos, uint mainTID)
    {
        var distribution = new TDistribution()
        {
            Attributions = [new(mainTID, "GameThread", CPUSetInfo.UniqueCores[0], CPUSetInfo.UniqueCores[0].Last() - CPUSetInfo.BeginCPUID)]
        };
        threadInfos = [..threadInfos.Where(x => x.TID != mainTID)];
        var usedCount = distribution.Attributions.Count;
        var maxAvailableCount = CPUSetInfo.PhysicalPCores.Count / 2;

        foreach (var distributionModel in _distributionModels)
        {
            if (threadInfos.Any(x => distributionModel.EnginePattern.IsMatch(x.Name)))
            {
                foreach (var threadPattern in distributionModel.ThreadPatterns)
                {
                    if (usedCount < maxAvailableCount)
                    {
                        distribution.Attributions.AddRange(threadInfos
                            .Where(x => threadPattern.IsMatch(x.Name))
                            .Take(maxAvailableCount - usedCount)
                            .Select(x =>
                            {
                                var cpuids = CPUSetInfo.UniqueCores[usedCount++];
                                return new TDistribution.Attribution(x.TID, x.Name, cpuids, cpuids[0] - CPUSetInfo.BeginCPUID);
                            }));
                    }
                    else break;
                }
                break;
            }
        };
        // distribution.SharedCPUIDs = [..CPUSetInfo.UniqueCores[usedCount..].SelectMany(x => x)];
        distribution.SharedCPUIDs = CPUSetInfo.PhysicalPECores[usedCount..];

        return distribution;
    }
}
