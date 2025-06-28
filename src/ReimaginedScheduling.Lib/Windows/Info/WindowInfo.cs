using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace ReimaginedScheduling.Lib.Windows.Info;

public class WindowInfo
{
    private readonly HWND hWindow;
    public bool isInvalid => hWindow.IsNull || !PInvoke.IsWindow(hWindow);
    public bool isValid => !isInvalid;

    public WindowInfo(IntPtr h_window)
    {
        hWindow = (HWND)h_window;
    }

    public static WindowInfo getMousePointWindow()
    {
        PInvoke.GetCursorPos(out var lpPoint);
        return new WindowInfo(PInvoke.WindowFromPoint(lpPoint));
    }
    
    public string currentName => Process.GetProcessById(ownerProcessId).MainWindowTitle;
    
    public (int width, int height) currentSize
    {
        get
        {
            PInvoke.GetClientRect(hWindow, out var lpRect);
            return (lpRect.Width, lpRect.Height);
        }
    }

    public unsafe int ownerProcessId
    {
        get
        {
            uint pid = 0;
            _ = PInvoke.GetWindowThreadProcessId(hWindow, &pid);
            return (int)pid;
        }
    }

    public unsafe int ownerThreadId => (int)PInvoke.GetWindowThreadProcessId(hWindow, null);
    
}
