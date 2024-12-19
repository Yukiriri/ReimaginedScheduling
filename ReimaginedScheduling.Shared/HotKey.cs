using Windows.System;
using Windows.Win32;

namespace ReimaginedScheduling.Shared;

public class HotKey
{
    public static bool IsKeyDown(VirtualKey keyCode) => (PInvoke.GetAsyncKeyState((int)keyCode) & 0x8000) != 0;

    public static bool IsCtrl => IsKeyDown(VirtualKey.Control);
    public static bool IsShift => IsKeyDown(VirtualKey.Shift);
}
