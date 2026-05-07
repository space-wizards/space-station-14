using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client._FinalStand.WaveHud;

public sealed class WaveHudOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly Texture[] _digits;
    private Font? _font;

    public int CurrentWave = 1;
    public int CurrentCredits = 0;

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
        var screenSize = _clyde.ScreenSize;

        // Wave counter — bottom-right, digit textures
        var waveStr = CurrentWave.ToString();
        var widths = new float[waveStr.Length];
        var totalWidth = 0f;
        for (var i = 0; i < waveStr.Length; i++)
        {
            var tex = _digits[waveStr[i] - '0'];
            widths[i] = tex.Width * (digitHeight / tex.Height);
            totalWidth += widths[i];
        }

        var x = screenSize.X - margin - totalWidth;
        var y = screenSize.Y - margin - digitHeight;

        for (var i = 0; i < waveStr.Length; i++)
        {
            var tex = _digits[waveStr[i] - '0'];
            screen.DrawTextureRect(tex, new UIBox2(x, y, x + widths[i], y + digitHeight));
            x += widths[i];
        }

        // Credits — bottom-left, text
        _font ??= new VectorFont(
            _resourceCache.GetResource<FontResource>(new ResPath("/Fonts/NotoSans/NotoSans-Bold.ttf")), 28);

        var creditsStr = $"${CurrentCredits:N0}";
        screen.DrawString(_font, new Vector2(margin, screenSize.Y - 220f), creditsStr, Color.Gold);
    }
}
