using ReimaginedScheduling.Common.Windows.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReimaginedScheduling.Common.Model;

public class TDistributionBuilder
{
    private struct DistributionModel
    {
        public Regex EnginePattern;
        public struct Fixed(int CoreIndex, Regex ThreadName)
        {
            public int CoreIndex = CoreIndex;
            public Regex ThreadName = ThreadName;
        }
        public Fixed[] Fixeds;
        public Regex[] Rotations;
    }
    private static readonly DistributionModel[] _distributionModels =
    [
        new DistributionModel()
        {
            EnginePattern = new(@"TaskGraphThread.P"), //UE4
            Fixeds =
            [
                // new(0, new(@"")),
                new(1, new(@"(RenderThread \d)|(RTHeartBeat \d)")),
                new(2, new(@"(RHIThread)|(Audio.*Thread)")),
            ],
            Rotations =
            [
                new(@"TaskGraphThread.P \d"),
                new(@"ThreadPool Thread-\d"),
            ]
        },
        new DistributionModel()
        {
            EnginePattern = new(@"ground Worker #"), //UE5
            Fixeds =
            [
                new(1, new(@"(RenderThread \d)|(RHISubmissionThread)")),
                new(2, new(@"RHIThread")),
            ],
            Rotations =
            [
                // new(@"ground Worker #\d(?!.+)"),
            ]
        },
        new DistributionModel()
        {
            EnginePattern = new(@"Unity"),
            Fixeds =
            [
                new(1, new(@"UnityMultiRenderingThread")),
                new(2, new(@"UnityGfxDeviceWorker")),
            ],
            Rotations =
            [
            ]
        },
        new DistributionModel()
        {
            EnginePattern = new(@"Render.*(T|t)hread"),
            Fixeds =
            [
                new(1, new(@"Render.*(T|t)hread")),
            ],
            Rotations = []
        },
    ];

    public static TDistribution Build(List<(uint TID, string TName)> threadInfos, uint mainTID)
    {
        var distribution = new TDistribution()
        {
            Attributions = [new(mainTID, "GameThread", CPUSetInfo.UniqueCores[0])]
        };
        var maxUsed = 0;
        foreach (var distributionModel in _distributionModels)
        {
            if (threadInfos.Where(x => distributionModel.EnginePattern.IsMatch(x.TName)).Any())
            {
                foreach (var fixeds in distributionModel.Fixeds)
                {
                    if (fixeds.CoreIndex < CPUSetInfo.PhysicalPCores.Count)
                    {
                        var minfos = threadInfos
                            .Where(x => fixeds.ThreadName.IsMatch(x.TName))
                            .Select(x => new TDistribution.Attribution(x.TID, x.TName, CPUSetInfo.UniqueCores[fixeds.CoreIndex]));
                        distribution.Attributions = [..distribution.Attributions, ..minfos];
                    }
                }
                maxUsed = Math.Min(distributionModel.Fixeds.Max(x => x.CoreIndex) + 1, CPUSetInfo.PhysicalPECores.Count - 1);
                // if (distributionModel.Fixeds.Max(x => x.CoreIndex) < CPUSetInfo.PhysicalPCores.Count)
                // {
                //     foreach (var rotation in distributionModel.Rotations)
                //     {
                //     }
                // }
                break;
            }
        }
        // distribution.SharedCPUIDs = CPUSetInfo.UniqueCores[maxUsed..].SelectMany(x => x).ToList();
        distribution.SharedCPUIDs = CPUSetInfo.PhysicalPECores[maxUsed..];

        return distribution;
    }
    
}
