using ReimaginedScheduling.Common.Windows.Device;
using System.Threading;
using Windows.System;

namespace ReimaginedScheduling.Common;

public class MyHotkey
{
    public static void WaitPress(params VirtualKey[] keyCodes)
    {
        for (; !HotKey.IsKeyDown(keyCodes); Thread.Sleep(1));
    }
}
