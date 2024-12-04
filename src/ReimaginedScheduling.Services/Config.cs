namespace ReimaginedScheduling.Services;

public static class Config
{
    public static string ConsoleSplitRow { get; private set; } = new('=', 110);

    public static int GPUUsageThreshold { get; set; } = 25;
    public static ulong GPUMemUsageThreshold { get; set; } = 1250;

    public static int TypicalPCoreCount { get; } = 8;
    public static int MaxTypicalPCoreCount { get; set; } = 16;

    public static int ThreadMonitorCount { get; set; } = 350;
    public static int ThreadSamplingCount { get; set; } = 6;
    public static int ThreadExclusiveThreshold { get; set; } = 40;

    //public static void Load()
    //{

    //}

    //public static void Save()
    //{

    //}

    //private static readonly string _fileName = "ReimaginedScheduling.Services.Config.json";
}
