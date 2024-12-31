using Windows.Win32;

namespace ReimaginedScheduling.Common.Windows.Info.Window;

public class DesktopWindowInfo : WindowInfo
{
    public DesktopWindowInfo() : base(PInvoke.GetDesktopWindow())
    {
    }
}
