using ReimaginedScheduling.Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.ToolHelp;
using Windows.Win32.System.Threading;

namespace ReimaginedScheduling.Services;

public class GameThreadManager(uint PID)
{
    public static uint OpenProcessAccess => (uint)(
        PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION |
        PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION |
        PROCESS_ACCESS_RIGHTS.PROCESS_SET_INFORMATION |
        PROCESS_ACCESS_RIGHTS.PROCESS_SET_LIMITED_INFORMATION);

    public static uint OpenThreadAccess => (uint)(
        THREAD_ACCESS_RIGHTS.THREAD_QUERY_INFORMATION |
        THREAD_ACCESS_RIGHTS.THREAD_QUERY_LIMITED_INFORMATION |
        THREAD_ACCESS_RIGHTS.THREAD_SET_INFORMATION |
        THREAD_ACCESS_RIGHTS.THREAD_SET_LIMITED_INFORMATION);

    public uint[] GetTIDs()
    {
        var TIDs = new List<uint>();
        unsafe
        {
            var hsnap = PInvoke.CreateToolhelp32Snapshot(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPTHREAD, 0);
            var te32 = new THREADENTRY32()
            {
                dwSize = (uint)sizeof(THREADENTRY32)
            };
            if (PInvoke.Thread32First(hsnap, &te32))
            {
                do
                {
                    if (te32.th32OwnerProcessID == _PID)
                    {
                        TIDs.Add(te32.th32ThreadID);
                    }
                } while (PInvoke.Thread32Next(hsnap, &te32));
            }
            PInvoke.CloseHandle(hsnap);
        }
        return [.. TIDs];
    }

    public static bool? ToggleScheduling(uint[] tids)
    {
        return null;
    }

    private readonly uint _PID = PID;
}
