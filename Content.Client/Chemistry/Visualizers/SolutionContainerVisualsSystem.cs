using Content.Client.Items.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.Visualizers;

public sealed class SolutionContainerVisualsSystem : VisualizerSystem<SolutionContainerVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SolutionContainerVisualsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SolutionContainerVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
        SubscribeLocalEvent<SolutionContainerVisualsComponent, GetEquipmentVisualsEvent>(OnGetClothingVisuals);
    }

    private void OnMapInit(EntityUid uid, SolutionContainerVisualsComponent component, MapInitEvent args)
    {
        component.InitialDescription = MetaData(uid).EntityDescription;
    }

    protected override void OnAppearanceChange(EntityUid uid, SolutionContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Check if the solution that was updated is the one set as represented
        if (!string.IsNullOrEmpty(component.SolutionName)
            && AppearanceSystem.TryGetData(uid, SolutionContainerVisuals.SolutionName, out string name, args.Component)
            && name != component.SolutionName)
            return;

        if (!AppearanceSystem.TryGetData(uid,
                SolutionContainerVisuals.FillFraction,
                out float fraction,
                args.Component))
            return;

        // C# moment; setting it as nullable (even though it's not) avoids a
        // gazillion .AsNullable calls.
        Entity<SpriteComponent?> ent = (uid, args.Sprite);
        if (!SpriteSystem.LayerMapTryGet(ent, component.Layer, out var fillLayer, false))
            return;

        var maxFillLevels = component.MaxFillLevels;
        var fillBaseName = component.FillBaseName;
        var changeColor = component.ChangeColor;
        var fillSprite = component.MetamorphicDefaultSprite;

        // Currently some solution methods such as overflowing will try to update appearance with a
        // volume greater than the max volume. We'll clamp it so players don't see
        // a giant error sign and error for debug.
        if (fraction > 1f)
        {
            Log.Error($"Attempted to set solution container visuals volume ratio on {ToPrettyString(uid)} to a "
                       + $"value greater than 1. Volume should never be greater than max volume!");
            fraction = 1f;
        }

        if (!component.Metamorphic)
            SpriteSystem.LayerSetVisible(ent, fillLayer, true);
        else
        {
            var reagentProto = MetamorphicChanged(uid, component, args, ent, fillLayer);

            if (reagentProto?.MetamorphicMaxFillLevels > 0)
            {
                SpriteSystem.LayerSetVisible(ent, fillLayer, true);
                maxFillLevels = reagentProto.MetamorphicMaxFillLevels;
                fillBaseName = reagentProto.MetamorphicFillBaseName;
                changeColor = reagentProto.MetamorphicChangeColor;
                fillSprite = reagentProto.MetamorphicSprite ?? fillSprite;
            }
            else
                SpriteSystem.LayerSetVisible(ent, fillLayer, false);
        }

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, maxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            if (fillBaseName == null)
                return;

            if (fillSprite != null)
                SpriteSystem.LayerSetSprite(ent, fillLayer, fillSprite);

            SpriteSystem.LayerSetRsiState(ent, fillLayer, fillBaseName + closestFillSprite);

            if (changeColor
                && AppearanceSystem.TryGetData(uid, SolutionContainerVisuals.Color, out Color color, args.Component))
                SpriteSystem.LayerSetColor(ent, fillLayer, color);
            else
                SpriteSystem.LayerSetColor(ent, fillLayer, Color.White);
        }
        else
        {
            if (component.EmptySpriteName == null)
                SpriteSystem.LayerSetVisible(ent, fillLayer, false);
            else
            {
                SpriteSystem.LayerSetRsiState(ent, fillLayer, component.EmptySpriteName);
                SpriteSystem.LayerSetColor(ent, fillLayer, changeColor ? component.EmptySpriteColor : Color.White);
            }
        }

        // in-hand visuals
        _itemSystem.VisualsChanged(uid);
    }

    private ReagentPrototype? MetamorphicChanged(EntityUid uid,
        SolutionContainerVisualsComponent component,
        AppearanceChangeEvent args,
        Entity<SpriteComponent?> ent,
        int fillLayer)
    {
        if (!AppearanceSystem.TryGetData(uid,
                SolutionContainerVisuals.BaseOverride,
                out string baseOverride,
                args.Component))
            return null;

        var reagentProto = _prototype.Index<ReagentPrototype>(baseOverride);

        if (SpriteSystem.LayerMapTryGet(ent, component.OverlayLayer, out var overlayLayer, false))
            SpriteSystem.LayerSetVisible(ent, overlayLayer, reagentProto.MetamorphicSprite is not null);

        if (!SpriteSystem.LayerMapTryGet(ent, component.BaseLayer, out var baseLayer, false))
            return null;

        if (reagentProto.MetamorphicSprite is { } sprite)
            SpriteSystem.LayerSetSprite(ent, baseLayer, sprite);
        else
        {
            SpriteSystem.LayerSetVisible(ent, fillLayer, true);
            if (component.MetamorphicDefaultSprite != null)
                SpriteSystem.LayerSetSprite(ent, baseLayer, component.MetamorphicDefaultSprite);
        }

        return reagentProto;
    }

    private void OnGetHeldVisuals(Entity<SolutionContainerVisualsComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (ent.Comp.InHandsFillBaseName == null)
            return;

        if (!TryComp<ItemComponent>(ent, out var item))
            return;

        var inhandPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand-";
        var layerKeyPrefix = inhandPrefix + args.Location.ToString().ToLowerInvariant() + ent.Comp.InHandsFillBaseName;

        if (GetVisualsLayer(ent, layerKeyPrefix, ent.Comp.InHandsMaxFillLevels) is { } layer)
            args.Layers.Add(layer);
    }

    private void OnGetClothingVisuals(Entity<SolutionContainerVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.EquippedFillBaseName == null)
            return;

        if (!TryComp<ClothingComponent>(ent, out var clothing))
            return;

        var equippedPrefix = clothing.EquippedPrefix == null
            ? $"equipped-{args.Slot}"
            : $" {clothing.EquippedPrefix}-equipped-{args.Slot}";
        var layerKeyPrefix = equippedPrefix + ent.Comp.EquippedFillBaseName;

        if (GetVisualsLayer(ent, layerKeyPrefix, ent.Comp.EquippedMaxFillLevels) is { } layer)
            args.Layers.Add(layer);
    }

    private (string Key, PrototypeLayerData Layer)? GetVisualsLayer(Entity<SolutionContainerVisualsComponent> ent,
        string layerKeyPrefix,
        int maxFillLevels)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return null;

        if (!AppearanceSystem.TryGetData<float>(ent,
                SolutionContainerVisuals.FillFraction,
                out var fraction,
                appearance))
            return null;

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, maxFillLevels + 1);
        if (closestFillSprite <= 0)
            return null;

        var layer = new PrototypeLayerData();
        var key = layerKeyPrefix + closestFillSprite;

        // Make sure the sprite state is valid so we don't show a big red error message
        // This saves us from having to make fill level sprites for every possible slot the item could be in (including pockets).
        if (!TryComp<SpriteComponent>(ent, out var sprite)
            || sprite.BaseRSI?.TryGetState(key, out _) != true)
            return null;

        layer.State = key;

        if (ent.Comp.ChangeColor
            && AppearanceSystem.TryGetData<Color>(ent, SolutionContainerVisuals.Color, out var color, appearance))
            layer.Color = color;

        return (key, layer);

    }
}
