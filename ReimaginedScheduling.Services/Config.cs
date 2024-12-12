using System;

namespace ReimaginedScheduling.Services;

public static class Config
{
    public static string ConsoleSplitRow => new('=', Console.WindowWidth);

    public static int GPUUsageThreshold { get; set; } = 25;
    public static ulong GPUMemUsageThreshold { get; set; } = 1250;

    //public static void Load()
    //{

    //}

    //public static void Save()
    //{

    //}

    //private static readonly string _fileName = "ReimaginedScheduling.Services.Config.json";
}
