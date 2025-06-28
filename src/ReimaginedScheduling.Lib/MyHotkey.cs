using Windows.System;
using ReimaginedScheduling.Lib.Windows.Device;

namespace ReimaginedScheduling.Lib;

public static class MyHotkey
{
    public static void waitDown(params VirtualKey[] key_codes)
    {
        for (; !key_codes.All(KeyState.isDown); Thread.Sleep(1));
    }
}
