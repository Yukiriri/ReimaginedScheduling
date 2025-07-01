using Windows.Win32;
using Windows.Win32.System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ReimaginedScheduling.Lib.Windows.Info;

public class ThreadBaseInfo
{
    protected readonly SafeFileHandle hThread;
    public int id { get; private set; }
    public bool isInvalid => hThread.IsInvalid;
    public bool isValid => !isInvalid;

    public ThreadBaseInfo(int thread_id)
    {
        id = thread_id;
        hThread = PInvoke.OpenThread_SafeHandle(
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION |
            THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION, false, (uint)id);
    }

    public static ThreadBaseInfo getCurrentThread() => new((int)PInvoke.GetCurrentThreadId());
    
    public int currentPriority
    {
        get => PInvoke.GetThreadPriority(hThread);
        set => PInvoke.SetThreadPriority(hThread, (THREAD_PRIORITY)value);
    }
    
    public unsafe string currentName
    {
        get
        {
            PInvoke.GetThreadDescription(hThread, out var ppszThreadDescription);
            var name = ppszThreadDescription.ToString() ?? "";
            PInvoke.LocalFree(new(ppszThreadDescription));
            return name;
        }
    }
    
    public ulong currentCycleTime
    {
        get
        {
            PInvoke.QueryThreadCycleTime(hThread, out var cycleTime);
            return cycleTime;
        }
    }

}
