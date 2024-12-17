using Windows.System;
using Windows.Win32;

namespace ReimaginedScheduling.Shared;

public class HotKey
{
    public static bool IsKeyDown(int keyCode) => (PInvoke.GetAsyncKeyState(keyCode) & 0x8000) != 0;

    public static bool IsCtrl => IsKeyDown((int)VirtualKey.Control);
    public static bool IsShift => IsKeyDown((int)VirtualKey.Shift);
    public static bool IsInsert => IsKeyDown((int)VirtualKey.Insert);
    public static bool IsPageUp => IsKeyDown((int)VirtualKey.PageUp);
    public static bool IsPageDown => IsKeyDown((int)VirtualKey.PageDown);

}
