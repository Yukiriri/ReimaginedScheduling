using Windows.System;
using Windows.Win32;

namespace ReimaginedScheduling.Common.Windows.Device;

public class HotKey
{
    public static bool IsKeyDown(VirtualKey keyCode)
    {
        return (PInvoke.GetAsyncKeyState((int)keyCode) & 0x8000) != 0;
    }

    public static bool IsKeyDown(VirtualKey[] keyCodes)
    {
        foreach (var keyCode in keyCodes)
        {
            if (!IsKeyDown(keyCode))
                return false;
        }
        return true;
    }
}
