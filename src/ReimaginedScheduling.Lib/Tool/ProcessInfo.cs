using System.Diagnostics;

namespace ReimaginedScheduling.Lib.Tool;

public static class ProcessInfo
{
    private static List<ProcessThread> listThreads(int process_id)
    {
        var process = Process.GetProcessById(process_id);
        var threads = new ProcessThread[process.Threads.Count];
        process.Threads.CopyTo(threads, 0);
        return [..threads];
    }
    
    public static List<int> listThreadIds(int process_id)
    {
        return [..listThreads(process_id).Select(x => x.Id)];
    }
    
}
