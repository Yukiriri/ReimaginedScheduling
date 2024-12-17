using Windows.Win32;
using Windows.Win32.Foundation;

namespace ReimaginedScheduling.Shared;

public class WindowInfo
{
    public bool SetForegroundHWND() => (_hWND = PInvoke.GetForegroundWindow()).IsNull;

    public bool SetDesktopHWND() => (_hWND = PInvoke.GetDesktopWindow()).IsNull;

    public unsafe string GetName()
    {
        string windowName = "";
        var textLength = PInvoke.GetWindowTextLength(_hWND) + 1;
        var text = new char[textLength];
        fixed (char* textPtr = text)
        {
            if (PInvoke.GetWindowText(_hWND, textPtr, textLength) > 0)
                windowName = new string(text);
        }
        return windowName;
    }

    public unsafe uint GetPID()
    {
        uint pid = 0;
        _ = PInvoke.GetWindowThreadProcessId(_hWND, &pid);
        return pid;
    }

    public unsafe uint GetTID()
    {
        var tid = PInvoke.GetWindowThreadProcessId(_hWND, null);
        return tid;
    }

    public unsafe (int Width, int Height) GetSize()
    {
        RECT rect = new();
        PInvoke.GetClientRect(_hWND, &rect);
        return (rect.Width, rect.Height);
    }

    private HWND _hWND = HWND.Null;
}
