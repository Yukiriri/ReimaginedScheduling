using Windows.System;
using Windows.Win32;

namespace ReimaginedScheduling.Lib.Windows.Device;

public static class KeyState
{
    public static bool isDown(VirtualKey key_code)
    {
        return (PInvoke.GetAsyncKeyState((int)key_code) & 0x8000) != 0;
    }
}
