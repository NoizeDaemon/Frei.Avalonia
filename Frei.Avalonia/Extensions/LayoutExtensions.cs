using Avalonia.Layout;
using Size = Avalonia.Size;

namespace Frei.Avalonia.Extensions;
public static class LayoutExtensions
{
    public static double GetLength(this Size size, Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Horizontal => size.Width,
            Orientation.Vertical => size.Height,
            _ => throw new NotSupportedException()
        };
    }

    public static Orientation GetPerpendicular(this Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Horizontal => Orientation.Vertical,
            Orientation.Vertical => Orientation.Horizontal,
            _ => throw new NotSupportedException()
        };
    }
}
