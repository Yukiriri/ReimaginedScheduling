using System.Drawing;
using Windows.Win32;

namespace ReimaginedScheduling.Common.Windows.Info.Window;

public class MousePointWindowInfo : WindowInfo
{
    static Point MousePoint
    {
        get
        {
            PInvoke.GetCursorPos(out var lpPoint);
            return lpPoint;
        }
    }
    public MousePointWindowInfo() : base(PInvoke.WindowFromPoint(MousePoint))
    {
    }
}
