using ReimaginedScheduling.Services.Utils;

namespace ReimaginedScheduling.Services
{
    public static class Config
    {
        static Config()
        {
            string s = ""; for (int i = 0; i < 100; i++) s += "=";
            ConsoleSplitRow = s;

            MaxExclusiveCount = CPUSetInfo.PhysicalPCoreList.Count;
            if (CPUSetInfo.IsPCoreOnly)
                MaxExclusiveCount /= 2;
        }

        public static string ConsoleSplitRow { get; private set; }
        public static int MaxExclusiveCount { get; private set; }
        public static int TypicalExclusiveCount { get; } = 8;

        public static int GPUUsageThreshold { get; set; } = 25;
        public static ulong GPUMemUsageThreshold { get; set; } = 1250;

        public static int MaxThreadMonitorCount { get; set; } = 350;
        public static int ThreadSamplingCount { get; set; } = 10;
        public static int ThreadUsageThreshold { get; set; } = 10;
        //public static int ThreadUsageOffsetThreshold { get; set; } = 10;

        //public static void Load()
        //{

        //}

        //public static void Save()
        //{

        //}

        //private static readonly string _fileName = "ReimaginedScheduling.Services.Config.json";
    }
}
