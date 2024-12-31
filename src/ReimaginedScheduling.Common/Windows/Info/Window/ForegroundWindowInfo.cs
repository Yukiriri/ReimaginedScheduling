using Windows.Win32;

namespace ReimaginedScheduling.Common.Windows.Info.Window;

public class ForegroundWindowInfo : WindowInfo
{
    public ForegroundWindowInfo() : base(PInvoke.GetForegroundWindow())
    {
    }
}
