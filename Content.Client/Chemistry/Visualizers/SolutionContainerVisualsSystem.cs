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
        var meta = MetaData(uid);
        component.InitialDescription = meta.EntityDescription;
    }

    protected override void OnAppearanceChange(EntityUid uid, SolutionContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        // Check if the solution that was updated is the one set as represented
        if (!string.IsNullOrEmpty(component.SolutionName))
        {
            if (AppearanceSystem.TryGetData<string>(uid, SolutionContainerVisuals.SolutionName, out var name,
                args.Component) && name != component.SolutionName)
            {
                return;
            }
        }

        if (!AppearanceSystem.TryGetData<float>(uid, SolutionContainerVisuals.FillFraction, out var fraction, args.Component))
            return;

        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(component.Layer, out var fillLayer))
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
            Log.Error("Attempted to set solution container visuals volume ratio on " + ToPrettyString(uid) + " to a value greater than 1. Volume should never be greater than max volume!");
            fraction = 1f;
        }
        if (component.Metamorphic)
        {
            if (args.Sprite.LayerMapTryGet(component.BaseLayer, out var baseLayer))
            {
                var hasOverlay = args.Sprite.LayerMapTryGet(component.OverlayLayer, out var overlayLayer);

                if (AppearanceSystem.TryGetData<string>(uid, SolutionContainerVisuals.BaseOverride,
                        out var baseOverride,
                        args.Component))
                {
                    _prototype.TryIndex<ReagentPrototype>(baseOverride, out var reagentProto);

                    if (reagentProto?.MetamorphicSprite is { } sprite)
                    {
                        args.Sprite.LayerSetSprite(baseLayer, sprite);
                        if (reagentProto.MetamorphicMaxFillLevels > 0)
                        {
                            args.Sprite.LayerSetVisible(fillLayer, true);
                            maxFillLevels = reagentProto.MetamorphicMaxFillLevels;
                            fillBaseName = reagentProto.MetamorphicFillBaseName;
                            changeColor = reagentProto.MetamorphicChangeColor;
                            fillSprite = sprite;
                        }
                        else
                            args.Sprite.LayerSetVisible(fillLayer, false);

                        if (hasOverlay)
                            args.Sprite.LayerSetVisible(overlayLayer, false);
                    }
                    else
                    {
                        args.Sprite.LayerSetVisible(fillLayer, true);
                        if (hasOverlay)
                            args.Sprite.LayerSetVisible(overlayLayer, true);
                        if (component.MetamorphicDefaultSprite != null)
                            args.Sprite.LayerSetSprite(baseLayer, component.MetamorphicDefaultSprite);
                    }
                }
            }
        }
        else
        {
            args.Sprite.LayerSetVisible(fillLayer, true);
        }

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, maxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            if (fillBaseName == null)
                return;

            var stateName = fillBaseName + closestFillSprite;
            if (fillSprite != null)
                args.Sprite.LayerSetSprite(fillLayer, fillSprite);
            args.Sprite.LayerSetState(fillLayer, stateName);

            if (changeColor && AppearanceSystem.TryGetData<Color>(uid, SolutionContainerVisuals.Color, out var color, args.Component))
                args.Sprite.LayerSetColor(fillLayer, color);
            else
                args.Sprite.LayerSetColor(fillLayer, Color.White);
        }
        else
        {
            if (component.EmptySpriteName == null)
                args.Sprite.LayerSetVisible(fillLayer, false);
            else
            {
                args.Sprite.LayerSetState(fillLayer, component.EmptySpriteName);
                if (changeColor)
                    args.Sprite.LayerSetColor(fillLayer, component.EmptySpriteColor);
                else
                    args.Sprite.LayerSetColor(fillLayer, Color.White);
            }
        }

        // in-hand visuals
        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, SolutionContainerVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (component.InHandsFillBaseName == null)
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        if (!TryComp<ItemComponent>(uid, out var item))
            return;

        if (!AppearanceSystem.TryGetData<float>(uid, SolutionContainerVisuals.FillFraction, out var fraction, appearance))
            return;

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, component.InHandsMaxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            var layer = new PrototypeLayerData();

            var heldPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand-";
            var key = heldPrefix + args.Location.ToString().ToLowerInvariant() + component.InHandsFillBaseName + closestFillSprite;

            layer.State = key;

            if (component.ChangeColor && AppearanceSystem.TryGetData<Color>(uid, SolutionContainerVisuals.Color, out var color, appearance))
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }

    private void OnGetClothingVisuals(Entity<SolutionContainerVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.EquippedFillBaseName == null)
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!TryComp<ClothingComponent>(ent, out var clothing))
            return;

        if (!AppearanceSystem.TryGetData<float>(ent, SolutionContainerVisuals.FillFraction, out var fraction, appearance))
            return;

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, ent.Comp.EquippedMaxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            var layer = new PrototypeLayerData();

            var equippedPrefix = clothing.EquippedPrefix == null ? $"equipped-{args.Slot}" : $" {clothing.EquippedPrefix}-equipped-{args.Slot}";
            var key = equippedPrefix + ent.Comp.EquippedFillBaseName + closestFillSprite;

            // Make sure the sprite state is valid so we don't show a big red error message
            // This saves us from having to make fill level sprites for every possible slot the item could be in (including pockets).
            if (!TryComp<SpriteComponent>(ent, out var sprite) || sprite.BaseRSI == null || !sprite.BaseRSI.TryGetState(key, out _))
                return;

            layer.State = key;

            if (ent.Comp.ChangeColor && AppearanceSystem.TryGetData<Color>(ent, SolutionContainerVisuals.Color, out var color, appearance))
                layer.Color = color;

            args.Layers.Add((key, layer));
        }
    }
}
