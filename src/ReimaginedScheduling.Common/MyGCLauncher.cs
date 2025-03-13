using System;
using System.Threading;

namespace ReimaginedScheduling.Common;

public class MyGCLauncher
{
    private static readonly Thread thread = new(new ThreadStart(delegate
    {
        for (;;Thread.Sleep(1000))
        {
            GC.Collect();
        }
    }));
    
    public static void Launch()
    {
        #if !DEBUG
        thread.Start();
        #endif
    }
}
