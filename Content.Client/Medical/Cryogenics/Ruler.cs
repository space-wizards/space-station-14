using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
namespace Content.Client.Medical.Cryogenics;

/// <summary>
/// A ruler specifically for the cryo pod window. Move it elsewhere if you want to use it for other purposes.
/// </summary>
public sealed class Ruler : Control
{
    public int TotalNotches = 10;
    public int MediumNotchInterval = 5;
    public int BigNotchInterval = 10;

    public float SmallNotchHeight = 0.1f;
    public float MediumNotchHeight = 0.25f;
    public float BigNotchHeight = 1f;

    public Color Color = new(1, 1, 1, 0.25f);

    protected override void Draw(DrawingHandleScreen handle)
    {
        var stepWidth = (float)PixelWidth / TotalNotches;

        for (int i = 0; i <= TotalNotches; i++)
        {
            var x = i * stepWidth;
            var height = (i % BigNotchInterval    == 0 ? BigNotchHeight :
                          i % MediumNotchInterval == 0 ? MediumNotchHeight :
                                                         SmallNotchHeight) * PixelHeight;
            var start = new Vector2(x, PixelHeight);
            var end = new Vector2(x, PixelHeight - height);
            handle.DrawLine(start, end, Color);
        }
    }
}
