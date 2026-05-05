using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._FinalStand.WaveHud;

public sealed class WaveHudOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly Texture[] _digits;

    public int CurrentWave = 1;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public WaveHudOverlay(Texture[] digits)
    {
        IoCManager.InjectDependencies(this);
        _digits = digits;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        const float digitHeight = 80f;
        const float margin = 24f;

        var screen = args.ScreenHandle;
        var waveStr = CurrentWave.ToString();

        var widths = new float[waveStr.Length];
        var totalWidth = 0f;
        for (var i = 0; i < waveStr.Length; i++)
        {
            var tex = _digits[waveStr[i] - '0'];
            widths[i] = tex.Width * (digitHeight / tex.Height);
            totalWidth += widths[i];
        }

        var screenSize = _clyde.ScreenSize;
        var x = screenSize.X - margin - totalWidth;
        var y = screenSize.Y - margin - digitHeight;

        for (var i = 0; i < waveStr.Length; i++)
        {
            var tex = _digits[waveStr[i] - '0'];
            screen.DrawTextureRect(tex, new UIBox2(x, y, x + widths[i], y + digitHeight));
            x += widths[i];
        }
    }
}
