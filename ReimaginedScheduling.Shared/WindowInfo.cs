using System.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace ReimaginedScheduling.Shared;

public class WindowInfo
{
    public WindowInfo(nint hWND)
    {
        _hWND = (HWND)hWND;
    }

    public bool IsValid => !IsInvalid;
    public bool IsInvalid => _hWND.IsNull || !PInvoke.IsWindow(_hWND);

    public unsafe string CurrentName
    {
        get
        {
            string windowName = "";
            var textLength = PInvoke.GetWindowTextLength(_hWND) + 1;
            var text = new char[textLength];
            fixed (char* textPtr = text)
            {
                if (PInvoke.GetWindowText(_hWND, textPtr, textLength) > 0)
                {
                    windowName = new string(textPtr);
                }
            }
            return windowName;
        }
    }

    public string GetDisplayName(int maxShowLength)
    {
        var windowName = CurrentName;
        var showLength = 0;
        while ((showLength = windowName.Aggregate(0, (length, next) => length + (next > 127 ? 2 : 1))) > maxShowLength)
        {
            windowName = windowName[0..(windowName.Length - 1)];
        }
        windowName += new string(' ', maxShowLength - showLength);
        return windowName;
    }

    public (int Width, int Height) GetSize()
    {
        PInvoke.GetClientRect(_hWND, out var lpRect);
        return (lpRect.Width, lpRect.Height);
    }

    public unsafe uint CurrentPID
    {
        get
        {
            uint pid = 0;
            _ = PInvoke.GetWindowThreadProcessId(_hWND, &pid);
            return pid;
        }
    }

    public unsafe uint CurrentTID
    {
        get => PInvoke.GetWindowThreadProcessId(_hWND, null);
    }

    private readonly HWND _hWND = HWND.Null;
}
