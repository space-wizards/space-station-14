using System.Numerics;
using Content.Shared.CF.Combat;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.CF.Hud;

/// <summary>
/// Two potion bottles for hit points and temporary hit points, with a shield for Armor Class.
/// Liquid is clipped to the clear interior of the bottle sprite.
/// </summary>
public sealed class CFHealthBall : Control
{
    private const string BottlePath = "/Textures/_CF/Interface/hud-bottle.png";

    private const float TextureWidth = 24f;
    private const float TextureHeight = 64f;
    private const float LiquidX = 4f;
    private const float LiquidY = 9f;
    private const float LiquidWidth = 16f;
    private const float LiquidHeight = 52f;

    private const float DisplayScale = 2f;
    private const float BottleGap = 8f;
    private const float ShieldHeight = 40f;
    private const float ShieldGap = 4f;

    [Dependency] private IResourceCache _cache = default!;

    private readonly Texture _bottle;
    private Font _font = default!;

    private float _hpFill = 1f;
    private float _tempFill;
    private float _shownHp = 1f;
    private float _shownTemp;
    private bool _shownReady;
    private float _phase;
    private int _currentHp;
    private int _maxHp;
    private int _tempHp;
    private int _armorClass = 10;
    private bool _offGuard;

    public CFHealthBall()
    {
        IoCManager.InjectDependencies(this);
        _bottle = _cache.GetResource<TextureResource>(BottlePath).Texture;
        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 14);
        var bottleW = TextureWidth * DisplayScale;
        var bottleH = TextureHeight * DisplayScale;
        MinSize = new Vector2(bottleW * 2f + BottleGap, ShieldHeight + ShieldGap + bottleH);
        MouseFilter = MouseFilterMode.Stop;
    }

    public void Set(int currentHp, int maxHp, int tempHp, int armorClass, bool offGuard)
    {
        _maxHp = Math.Max(0, maxHp);
        _currentHp = Math.Max(0, currentHp);
        _tempHp = Math.Max(0, tempHp);
        _armorClass = armorClass;
        _offGuard = offGuard;
        _hpFill = _maxHp <= 0 ? 0f : Math.Clamp(_currentHp / (float) _maxHp, 0f, 1f);
        _tempFill = _maxHp <= 0 ? 0f : Math.Clamp(_tempHp / (float) _maxHp, 0f, 1f);
        if (!_shownReady)
        {
            _shownHp = _hpFill;
            _shownTemp = _tempFill;
            _shownReady = true;
        }

        if (_offGuard)
        {
            ToolTip = Loc.GetString("cf-hud-ball-tooltip-off-guard",
                ("current", _currentHp),
                ("max", _maxHp),
                ("temp", _tempHp),
                ("ac", _armorClass),
                ("reduced", _armorClass - CFFlanking.OffGuardAcPenalty));
        }
        else
        {
            ToolTip = Loc.GetString("cf-hud-ball-tooltip",
                ("current", _currentHp),
                ("max", _maxHp),
                ("temp", _tempHp),
                ("ac", _armorClass));
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        var blend = Math.Min(1f, args.DeltaSeconds * 6f);
        _shownHp += (_hpFill - _shownHp) * blend;
        _shownTemp += (_tempFill - _shownTemp) * blend;
        _phase += args.DeltaSeconds * 2.4f;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var scale = PixelSize.Y / MinSize.Y;
        if (scale <= 0f)
            return;

        var bottleW = TextureWidth * DisplayScale * scale;
        var bottleH = TextureHeight * DisplayScale * scale;
        var gap = BottleGap * scale;
        var shieldH = ShieldHeight * scale;
        var top = shieldH + ShieldGap * scale;
        var left = (PixelSize.X - (bottleW * 2f + gap)) / 2f;

        DrawShield(handle, new UIBox2(left + bottleW * 0.35f, 0f, left + bottleW + gap + bottleW * 0.65f, shieldH));
        DrawBottle(handle, new UIBox2(left, top, left + bottleW, top + bottleH), _shownHp, _currentHp,
            new Color(176, 28, 32), new Color(230, 96, 88, 200));
        var right = left + bottleW + gap;
        DrawBottle(handle, new UIBox2(right, top, right + bottleW, top + bottleH), _shownTemp, _tempHp,
            new Color(36, 96, 196), new Color(140, 196, 255, 200));
    }

    private void DrawBottle(DrawingHandleScreen handle, UIBox2 box, float fill, int amount, Color body, Color crest)
    {
        var pixel = box.Width / TextureWidth;
        var liquid = new UIBox2(
            box.Left + LiquidX * pixel,
            box.Top + LiquidY * pixel,
            box.Left + (LiquidX + LiquidWidth) * pixel,
            box.Top + (LiquidY + LiquidHeight) * pixel);

        handle.DrawRect(liquid, new Color(12, 14, 18, 210));
        DrawLiquid(handle, liquid, fill, body, crest, pixel);
        handle.DrawTextureRect(_bottle, box);
        DrawCentered(handle, _font, amount.ToString(), liquid.Center, pixel / DisplayScale, Color.White);
    }

    private void DrawLiquid(DrawingHandleScreen handle, UIBox2 liquid, float fill, Color body, Color crest, float pixel)
    {
        var left = (int) MathF.Floor(liquid.Left);
        var right = (int) MathF.Ceiling(liquid.Right);
        var bottom = liquid.Bottom;
        var waveAmp = 2.2f * pixel;
        for (var x = left; x < right; x++)
        {
            var wave = liquid.Top + liquid.Height * (1f - fill) + MathF.Sin((x * 0.18f) + _phase) * waveAmp;
            var liquidTop = MathF.Max(liquid.Top, wave);
            if (liquidTop < bottom)
                handle.DrawRect(UIBox2.FromDimensions(x, liquidTop, 1, bottom - liquidTop), body);

            if (wave > liquid.Top && wave < bottom)
                handle.DrawRect(UIBox2.FromDimensions(x, wave - pixel, 1, pixel * 2f), crest);
        }
    }

    private void DrawShield(DrawingHandleScreen handle, UIBox2 box)
    {
        var cx = (box.Left + box.Right) / 2f;
        var shoulder = box.Top + box.Height * 0.22f;
        var waist = box.Top + box.Height * 0.62f;
        var fill = new Color(24, 28, 36, 235);
        var ink = new Color(232, 206, 140);
        var top = (int) MathF.Floor(box.Top);
        var bottom = (int) MathF.Ceiling(box.Bottom);
        for (var y = top; y < bottom; y++)
        {
            var sample = y + 0.5f;
            float half;
            if (sample <= shoulder)
                half = box.Width * 0.5f * ((sample - box.Top) / MathF.Max(1f, shoulder - box.Top));
            else if (sample >= waist)
                half = box.Width * 0.5f * ((box.Bottom - sample) / MathF.Max(1f, box.Bottom - waist));
            else
                half = box.Width * 0.5f;

            if (half > 0.5f)
                handle.DrawRect(UIBox2.FromDimensions(cx - half, y, half * 2f, 1f), fill);
        }

        var peak = new Vector2(cx, box.Top);
        var upperLeft = new Vector2(box.Left, shoulder);
        var upperRight = new Vector2(box.Right, shoulder);
        var lowerLeft = new Vector2(box.Left, waist);
        var lowerRight = new Vector2(box.Right, waist);
        var point = new Vector2(cx, box.Bottom);
        handle.DrawLine(peak, upperRight, ink);
        handle.DrawLine(upperRight, lowerRight, ink);
        handle.DrawLine(lowerRight, point, ink);
        handle.DrawLine(point, lowerLeft, ink);
        handle.DrawLine(lowerLeft, upperLeft, ink);
        handle.DrawLine(upperLeft, peak, ink);
        var center = box.Center + new Vector2(0f, box.Height * 0.06f);
        if (_offGuard)
            DrawOffGuardAc(handle, center, box.Width);
        else
            DrawCentered(handle, _font, _armorClass.ToString(), center, 1f, Color.White);
    }

    private void DrawOffGuardAc(DrawingHandleScreen handle, Vector2 center, float maxWidth)
    {
        var original = _armorClass.ToString();
        var reduced = (_armorClass - CFFlanking.OffGuardAcPenalty).ToString();
        var gap = 6f;
        var scale = 1f;
        var originalSize = handle.GetDimensions(_font, original, scale);
        var reducedSize = handle.GetDimensions(_font, reduced, scale);
        var total = originalSize.X + gap + reducedSize.X;
        var limit = maxWidth * 0.85f;
        if (total > limit && originalSize.X + reducedSize.X > 0f)
        {
            scale = MathF.Max(0.55f, (limit - gap) / (originalSize.X + reducedSize.X));
            originalSize = handle.GetDimensions(_font, original, scale);
            reducedSize = handle.GetDimensions(_font, reduced, scale);
            total = originalSize.X + gap + reducedSize.X;
        }

        var left = center.X - total / 2f;
        var struck = new Color(196, 196, 196);
        var originalPos = new Vector2(left, center.Y - originalSize.Y / 2f);
        handle.DrawString(_font, originalPos, original, scale, struck);
        var midY = originalPos.Y + originalSize.Y * 0.55f;
        handle.DrawLine(new Vector2(originalPos.X, midY), new Vector2(originalPos.X + originalSize.X, midY), struck);

        var reducedPos = new Vector2(left + originalSize.X + gap, center.Y - reducedSize.Y / 2f);
        handle.DrawString(_font, reducedPos, reduced, scale, new Color(220, 48, 48));
    }

    private void DrawCentered(DrawingHandleScreen handle, Font font, string text, Vector2 at, float scale, Color color)
    {
        var size = handle.GetDimensions(font, text, scale);
        handle.DrawString(font, at - size / 2f, text, scale, color);
    }
}
