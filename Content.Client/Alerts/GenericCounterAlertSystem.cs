using System.Numerics;
using Content.Shared.Alert.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Alerts;

/// <summary>
/// This handles <see cref="GenericCounterAlertComponent"/>
/// </summary>
public sealed class GenericCounterAlertSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GenericCounterAlertComponent, UpdateAlertSpriteEvent>(OnUpdateAlertSprite);
    }

    private void OnUpdateAlertSprite(Entity<GenericCounterAlertComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        var sprite = args.SpriteViewEnt.Comp;

        var ev = new GetGenericAlertCounterAmountEvent();
        RaiseLocalEvent(args.ViewerEnt, ref ev);

        if (!ev.Handled)
            return;

        var digitCount = GetDigitCount((ent, ent, sprite));

        // Clamp it to a positive number that we can actually display in full (no rollover to 0)
        var amount = (int) Math.Clamp(ev.Amount!.Value, 0, Math.Pow(10, digitCount) - 1);

        var baseOffset = ((ent.Comp.AlertSize.X - (digitCount * ent.Comp.GlyphWidth)) / 2f) * (1f / EyeManager.PixelsPerMeter);
        for (var i = 0; i < ent.Comp.DigitKeys.Count; i++)
        {
            if (!sprite.LayerMapTryGet(ent.Comp.DigitKeys[i], out var layer))
                continue;

            var result = amount / (int) Math.Pow(10, i) % 10;
            sprite.LayerSetState(layer, $"{result}");

            if (ent.Comp.CenterGlyph)
            {
                var littleOFfset = ((digitCount - i) * ent.Comp.GlyphWidth * (1f / EyeManager.PixelsPerMeter));
                var offset = baseOffset + littleOFfset;
                Log.Debug($"fuck[{i}]: {baseOffset}, {littleOFfset}");
                sprite.LayerSetOffset(layer, new Vector2(offset, 0));
            }
        }
    }

    /// <summary>
    /// Gets the number of digits that we can display.
    /// </summary>
    /// <returns></returns>
    private int GetDigitCount(Entity<GenericCounterAlertComponent, SpriteComponent> ent)
    {
        var comp = ent.Comp1;
        var sprite = ent.Comp2;

        for (var i = comp.DigitKeys.Count - 1; i >= 0; i--)
        {
            if (sprite.LayerExists(comp.DigitKeys[i]))
                return i + 1;
        }

        return 0;
    }
}
