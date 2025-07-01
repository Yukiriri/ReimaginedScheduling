using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ReimaginedScheduling.Lib.Windows.Info;

public class ProcessBaseInfo
{
    protected readonly SafeFileHandle hProcess;
    public int id { get; private set; }
    public string exeName { get; private set; }
    public bool isInvalid => hProcess.IsInvalid;
    public bool isValid => !isInvalid;

    public ProcessBaseInfo(int process_id)
    {
        id = process_id;
        hProcess = PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION |
            PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION, false, (uint)id);
        exeName = string.Empty;
        try
        {
            exeName = $"{Process.GetProcessById(id).ProcessName}.exe";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static ProcessBaseInfo getCurrentProcess() => new((int)PInvoke.GetCurrentProcessId());
    
    public uint currentPriority
    {
        get => PInvoke.GetPriorityClass(hProcess);
        set => PInvoke.SetPriorityClass(hProcess, (PROCESS_CREATION_FLAGS)value);
    }

    public ulong currentCycleTime
    {
        get
        {
            PInvoke.QueryProcessCycleTime(hProcess, out var cycleTime);
            return cycleTime;
        }
    }

    private static List<ProcessThread> listThreads(int process_id)
    {
        var process = Process.GetProcessById(process_id);
        var threads = new ProcessThread[process.Threads.Count];
        if (threads.Length > 0)
        {
            process.Threads.CopyTo(threads, 0);
        }
        return [..threads];
    }
    
    public static List<int> listThreadIds(int process_id)
    {
        return [..listThreads(process_id).Select(x => x.Id)];
    }

}
