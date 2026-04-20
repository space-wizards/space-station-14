using Content.Client.Items.Systems;
using Content.Client.Storage.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

public sealed class ItemSlotVisualsSystem : VisualizerSystem<ItemSlotVisualsComponent>
{
    [Dependency] private readonly ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemSlotVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
        SubscribeLocalEvent<ItemSlotVisualsComponent, GetEquipmentVisualsEvent>(OnGetClothingVisuals);
    }

    protected override void OnAppearanceChange(EntityUid uid, ItemSlotVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, ItemSlotVisualLayers.ContainsItem, out var contains, args.Component))
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.FillLayer, out var fillLayer, false))
            return;

        var closestFillSprite = ContentHelpers.RoundToLevels(contains, 1, component.MaxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            if (component.FillBaseName == null)
                return;

            SpriteSystem.LayerSetVisible((uid, args.Sprite), fillLayer, true);
            var stateName = component.FillBaseName + closestFillSprite;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), fillLayer, stateName);
        }
        else
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), fillLayer, false);
        }

        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, ItemSlotVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (component.InHandsFillBaseName == null)
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        if (!TryComp<ItemComponent>(uid, out var item))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, ItemSlotVisualLayers.ContainsItem, out var contains, appearance))
            return;

        var closestFillSprite = ContentHelpers.RoundToLevels(contains, 1, component.InHandsMaxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            var layer = new PrototypeLayerData();

            var heldPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand-";
            var key = heldPrefix + args.Location.ToString().ToLowerInvariant() + component.InHandsFillBaseName + closestFillSprite;

            layer.State = key;

            args.Layers.Add((key, layer));
        }
    }

    private void OnGetClothingVisuals(Entity<ItemSlotVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.EquippedFillBaseName == null)
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!TryComp<ClothingComponent>(ent, out var clothing))
            return;

        if (!AppearanceSystem.TryGetData<int>(ent, ItemSlotVisualLayers.ContainsItem, out var contains, appearance))
            return;

        var closestFillSprite = ContentHelpers.RoundToLevels(contains, 1, ent.Comp.EquippedMaxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            var layer = new PrototypeLayerData();

            var equippedPrefix = clothing.EquippedPrefix == null
                ? $"equipped-{args.Slot}"
                : $" {clothing.EquippedPrefix}-equipped-{args.Slot}";
            var key = equippedPrefix + ent.Comp.EquippedFillBaseName + closestFillSprite;

            if (!TryComp<SpriteComponent>(ent, out var sprite) || sprite.BaseRSI == null ||
                !sprite.BaseRSI.TryGetState(key, out _))
                return;

            layer.State = key;

            args.Layers.Add((key, layer));
        }
    }
}
