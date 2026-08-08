// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System;
using System.Numerics;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.UserInterface.RichText;

public sealed class ShiftTag : IMarkupTagHandler
{
    public string Name => "shift";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        PaperFormattingTagHelpers.PushEffects(
            context,
            PaperFormattingTagHelpers.GetCurrentEffects(context) with { TightLineSpacing = true });
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

public sealed class SmallTag : IMarkupTagHandler
{
    private const int MaxDecrease = 6;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "small";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var decrease = (int) Math.Clamp(node.Value.LongValue ?? 1, 1, MaxDecrease);
        node.Attributes["size"] = new MarkupParameter(FontTag.DefaultSize - decrease);
        var sourceFont = PaperFormattingTagHelpers.GetCurrentFont(context);
        var effects = PaperFormattingFont.GetEffects(sourceFont);

        var font = FontTag.CreateFont(
            context.Font,
            node,
            _resourceCache,
            _prototypeManager,
            FontTag.DefaultFont);

        context.Font.Push(PaperFormattingFont.WithEffects(
            font,
            effects,
            sourceFont));
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

public sealed class ConfusionTag : IMarkupTagHandler
{
    private const int MaxStrength = 10;

    public string Name => "conf";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        PushConfusion(node, context);
    }

    internal static void PushConfusion(MarkupNode node, MarkupDrawingContext context)
    {
        var strength = (int) Math.Clamp(node.Value.LongValue ?? 1, 1, MaxStrength);
        PaperFormattingTagHelpers.PushEffects(
            context,
            PaperFormattingTagHelpers.GetCurrentEffects(context) with
            {
                ConfusionStrength = strength,
            });
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

public sealed class CyrillicConfusionTag : IMarkupTagHandler
{
    public string Name => "сonf";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        ConfusionTag.PushConfusion(node, context);
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

public sealed class CutTag : IMarkupTagHandler
{
    public string Name => "cut";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        PaperFormattingTagHelpers.PushEffects(
            context,
            PaperFormattingTagHelpers.GetCurrentEffects(context) with { Strikethrough = true });
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

public sealed class UnderlineTag : IMarkupTagHandler
{
    public string Name => "uline";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        PaperFormattingTagHelpers.PushEffects(
            context,
            PaperFormattingTagHelpers.GetCurrentEffects(context) with { Underline = true });
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}

internal readonly record struct PaperTextEffects(
    bool TightLineSpacing = false,
    int ConfusionStrength = 0,
    bool Strikethrough = false,
    bool Underline = false);

internal static class PaperFormattingTagHelpers
{
    public static Font GetCurrentFont(MarkupDrawingContext context)
    {
        if (!context.Font.TryPeek(out var font))
            throw new InvalidOperationException("A paper formatting tag requires a font in the drawing context.");

        return font;
    }

    public static PaperTextEffects GetCurrentEffects(MarkupDrawingContext context)
    {
        return PaperFormattingFont.GetEffects(GetCurrentFont(context));
    }

    public static void PushEffects(MarkupDrawingContext context, PaperTextEffects effects)
    {
        var font = GetCurrentFont(context);
        context.Font.Push(PaperFormattingFont.WithEffects(font, effects));
    }
}

internal sealed class PaperFormattingFont : Font
{
    private readonly Font _inner;
    private readonly PaperTextEffects _effects;
    private readonly HandwritingSequence _sequence;

    private PaperFormattingFont(Font inner, PaperTextEffects effects, HandwritingSequence sequence)
    {
        _inner = inner;
        _effects = effects;
        _sequence = sequence;
    }

    public static PaperTextEffects GetEffects(Font font)
    {
        return font is PaperFormattingFont formattingFont
            ? formattingFont._effects
            : default;
    }

    public static Font WithEffects(Font font, PaperTextEffects effects, Font? sequenceSource = null)
    {
        var current = font as PaperFormattingFont;
        var source = sequenceSource as PaperFormattingFont ?? current;
        var inner = current != null
            ? current._inner
            : font;
        if (effects == default)
            return inner;

        var sequence = source != null &&
            source._effects.ConfusionStrength > 0 &&
            effects.ConfusionStrength > 0
                ? source._sequence
                : new HandwritingSequence();
        return new PaperFormattingFont(inner, effects, sequence);
    }

    public override int GetAscent(float scale)
    {
        return _inner.GetAscent(scale);
    }

    public override int GetHeight(float scale)
    {
        return _inner.GetHeight(scale);
    }

    public override int GetDescent(float scale)
    {
        return _inner.GetDescent(scale);
    }

    public override int GetLineHeight(float scale)
    {
        // Full-block glyphs fill the ascent but not the descender area.
        return _effects.TightLineSpacing
            ? _inner.GetAscent(scale)
            : _inner.GetLineHeight(scale);
    }

    public override float DrawChar(
        DrawingHandleBase handle,
        Rune rune,
        Vector2 baseline,
        float scale,
        Color color,
        bool fallback = true)
    {
        if (_effects.ConfusionStrength <= 0 || rune.Value == '\n' || rune.Value == '\r')
        {
            var plainAdvance = _inner.DrawChar(handle, rune, baseline, scale, color, fallback);
            DrawDecorations(handle, baseline, plainAdvance, scale, color);
            return plainAdvance;
        }

        var variation = GetHandwritingVariation(rune, _sequence.DrawRuneIndex++);
        var drawOffset = new Vector2(
            MathF.Round(variation.OffsetX * scale),
            MathF.Round(variation.OffsetY * scale));
        var drawPosition = baseline + drawOffset;
        var widthScale = variation.WidthPermille / 1000f;
        var heightScale = variation.HeightPermille / 1000f;
        var rotation = variation.RotationTenths * MathF.PI / 1800f;
        var originalTransform = handle.GetTransform();
        var characterTransform = Matrix3x2.CreateScale(new Vector2(widthScale, heightScale), drawPosition) *
            Matrix3x2.CreateRotation(rotation, drawPosition);
        handle.SetTransform(characterTransform * originalTransform);

        float advance;
        try
        {
            advance = _inner.DrawChar(handle, rune, drawPosition, scale, color, fallback);
        }
        finally
        {
            handle.SetTransform(originalTransform);
        }

        if (advance > 0)
        {
            advance = MathF.Max(
                1f,
                MathF.Round(advance * widthScale) + MathF.Round(variation.Spacing * scale));
        }

        DrawDecorations(handle, baseline, advance, scale, color);
        return advance;
    }

    public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback = true)
    {
        if (_effects.ConfusionStrength <= 0 || rune.Value == '\n' || rune.Value == '\r')
            return _inner.GetCharMetrics(rune, scale, fallback);

        var variation = GetHandwritingVariation(rune, _sequence.MeasureRuneIndex++);
        var metrics = _inner.GetCharMetrics(rune, scale, fallback);
        if (metrics == null || metrics.Value.Advance <= 0)
            return metrics;

        var widthScale = variation.WidthPermille / 1000f;
        var heightScale = variation.HeightPermille / 1000f;
        var advance = Math.Max(
            1,
            (int) MathF.Round(metrics.Value.Advance * widthScale) +
            (int) MathF.Round(variation.Spacing * scale));
        return new CharMetrics(
            (int) MathF.Round(metrics.Value.BearingX * widthScale),
            (int) MathF.Round(metrics.Value.BearingY * heightScale),
            advance,
            (int) MathF.Round(metrics.Value.Width * widthScale),
            (int) MathF.Round(metrics.Value.Height * heightScale));
    }

    private HandwritingVariation GetHandwritingVariation(Rune rune, int runeIndex)
    {
        var strength = _effects.ConfusionStrength;
        // Fixed integer hashes keep the handwriting identical on every client.
        var characterHash = MixHash(unchecked((uint) rune.Value * 0x85EBCA6Bu ^ ((uint) runeIndex + 1) * 0x9E3779B9u));

        // The baseline drifts smoothly while individual glyphs retain small imperfections.
        var maxVerticalOffset = Math.Max(1, (strength + 1) / 2);
        var strokeOffset = GetBaselineDrift(runeIndex, Math.Max(1, (strength + 2) / 3));
        var detailOffset = GetSignedOffset(characterHash, strength / 5);
        var offsetY = Math.Clamp(strokeOffset + detailOffset, -maxVerticalOffset, maxVerticalOffset);

        var offsetX = GetSignedOffset(MixHash(unchecked(characterHash + 0x68E31DA4u)), strength / 5);
        var spacing = GetSignedOffset(MixHash(unchecked(characterHash + 0xB5297A4Du)), strength / 4) -
            (strength + 1) / 6;
        var widthPermille = 1000 + GetSignedOffset(
            MixHash(unchecked(characterHash + 0x1B56C4E9u)),
            strength * 4);
        var heightPermille = 1000 + GetSignedOffset(
            MixHash(unchecked(characterHash + 0xC2B2AE35u)),
            strength * 5);
        var rotationTenths = GetSignedOffset(
            MixHash(unchecked(characterHash + 0x27D4EB2Fu)),
            strength * 2);

        return new HandwritingVariation(
            offsetX,
            offsetY,
            spacing,
            widthPermille,
            heightPermille,
            rotationTenths);
    }

    private static int GetBaselineDrift(int runeIndex, int amplitude)
    {
        const int strokeLength = 3;
        var stroke = runeIndex / strokeLength;
        var position = runeIndex % strokeLength;
        var from = GetSignedOffset(
            MixHash(unchecked((uint) stroke * 0x9E3779B9u + 0xA511E9B3u)),
            amplitude);
        var to = GetSignedOffset(
            MixHash(unchecked(((uint) stroke + 1) * 0x9E3779B9u + 0xA511E9B3u)),
            amplitude);
        return (from * (strokeLength - position) + to * position) / strokeLength;
    }

    private static int GetSignedOffset(uint hash, int amplitude)
    {
        if (amplitude <= 0)
            return 0;

        return (int) (hash % (uint) (amplitude * 2 + 1)) - amplitude;
    }

    private void DrawDecorations(
        DrawingHandleBase handle,
        Vector2 baseline,
        float advance,
        float scale,
        Color color)
    {
        if (advance <= 0)
            return;

        var endX = baseline.X + advance;

        if (_effects.Strikethrough)
        {
            var y = MathF.Round(baseline.Y - _inner.GetAscent(scale) * 0.35f);
            handle.DrawLine(new Vector2(baseline.X, y), new Vector2(endX, y), color);
        }

        if (_effects.Underline)
        {
            var y = MathF.Round(baseline.Y + MathF.Max(1f, _inner.GetDescent(scale) * 0.5f));
            handle.DrawLine(new Vector2(baseline.X, y), new Vector2(endX, y), color);
        }
    }

    private static uint MixHash(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }

    private readonly record struct HandwritingVariation(
        int OffsetX,
        int OffsetY,
        int Spacing,
        int WidthPermille,
        int HeightPermille,
        int RotationTenths);

    private sealed class HandwritingSequence
    {
        public int DrawRuneIndex;
        public int MeasureRuneIndex;
    }
}
