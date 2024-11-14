using ReimaginedScheduling.Services.Utils;

namespace ReimaginedScheduling.Services
{
    public static class Config
    {
        static Config()
        {
            MaxExclusiveCount = CPUSetInfo.PhysicalPCoreList.Count;
            if (CPUSetInfo.IsPCoreOnly)
                MaxExclusiveCount /= 2;
        }

        public static string ConsoleSplitRow { get; } = "====================================================================================================";
        public static int MaxExclusiveCount { get; private set; }
        public static int TypicalExclusiveCount { get; } = 8;

        public static long GPUUsageThreshold { get; set; } = 33;
        public static long GPUMemUsageThreshold { get; set; } = 1250;

        public static int MaxThreadMonitorCount { get; set; } = 350;
        public static int ThreadSamplingCount { get; set; } = 10;
        public static int ThreadAntiJitterUsageThreshold { get; set; } = 20;

        //public static void Load()
        //{

        //}

        //public static void Save()
        //{

        //}

        //private static readonly string _fileName = "ReimaginedScheduling.Services.Config.json";
    }
}
