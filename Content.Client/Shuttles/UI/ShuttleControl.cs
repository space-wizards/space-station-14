using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Provides common functionality for radar-like displays on shuttle consoles.
/// </summary>
public abstract class ShuttleControl : MapGridControl
{
    protected static readonly Color BackingColor = Color.FromHex("#1e1e22");

    protected ShuttleControl(float minRange, float maxRange, float range) : base(minRange, maxRange, range)
    {
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var backing = Color.FromHex("#1e1e22");
        handle.DrawRect(new UIBox2(0f, this.Height, this.Width, 0f), backing);

        var lineCount = 4f;
        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var zoom = MinimapScale;
        lineCount /= zoom;

        /*
        for (var i = 1; i < lineCount; i++)
        {
            var origin = Width / lineCount * i;
            handle.DrawLine(new Vector2(origin, 0f), new Vector2(origin, Height), gridLines);
            handle.DrawLine(new Vector2(0f, origin), new Vector2(Width, origin), gridLines);
        }
        */
    }
}
