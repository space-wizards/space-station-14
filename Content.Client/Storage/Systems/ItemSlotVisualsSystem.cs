using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Containers.ItemSlot;
using Content.Shared.Hands;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

public sealed partial class ItemSlotVisualsSystem : VisualizerSystem<ItemSlotVisualsComponent>
{
    [Dependency] private ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Have these systems go first & add their visuals, then after that, we add our own. No more conflicting visuals!
        SubscribeLocalEvent<ItemSlotVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) });
        SubscribeLocalEvent<ItemSlotVisualsComponent, GetEquipmentVisualsEvent>(OnGetClothingVisuals, after: new[] { typeof(ClothingSystem) });
    }

    protected override void OnAppearanceChange(EntityUid uid, ItemSlotVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        foreach (var visual in component.SlotVisuals.Values)
        {
            if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), visual.Layer, out var layerIndex, false))
                continue;

            var filled = AppearanceSystem.TryGetData(uid, visual.Layer, out bool hasItem, args.Component) && hasItem;

            if (filled && !string.IsNullOrEmpty(visual.FillBaseName))
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, true);
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), layerIndex, visual.FillBaseName);
            }
            else
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, false);
            }
        }

        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(Entity<ItemSlotVisualsComponent> ent, ref GetInhandVisualsEvent args)
    {
        foreach (var visual in ent.Comp.SlotVisuals.Values)
        {
            if (visual.InHandsFillBaseName == null)
                continue;

            if (!TryComp<ItemComponent>(ent, out var item))
                return;

            var heldPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand-";

            // No need for fillLevels if it'll just fit one item.
            var layerKeyPrefix = heldPrefix + args.Location.ToString().ToLowerInvariant() + visual.InHandsFillBaseName;

            if (GetVisualsLayer(ent, layerKeyPrefix) is not { } layer)
                return;

            args.Layers.Add(layer);
        }
    }

    private void OnGetClothingVisuals(Entity<ItemSlotVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        foreach (var visual in ent.Comp.SlotVisuals.Values)
        {
            if (visual.EquippedFillBaseName == null)
                continue;

            if (!TryComp<ClothingComponent>(ent, out var clothing))
                return;

            var equippedPrefix = clothing.EquippedPrefix == null ? $"equipped-{args.Slot}" : $"{clothing.EquippedPrefix}-equipped-{args.Slot}";
            var layerKeyPrefix = equippedPrefix + visual.EquippedFillBaseName;

            if (GetVisualsLayer(ent, layerKeyPrefix) is not { } layer)
                return;

            args.Layers.Add(layer);
        }
    }

    private (string Key, PrototypeLayerData Layer)? GetVisualsLayer(Entity<ItemSlotVisualsComponent> ent, string layerKeyPrefix)
    {
        foreach (var visual in ent.Comp.SlotVisuals.Values)
        {
            if (!TryComp<AppearanceComponent>(ent, out var appearance)
                || !AppearanceSystem.TryGetData(ent, visual.Layer, out bool hasItem, appearance)
                || !hasItem)
                return null;

            var layer = new PrototypeLayerData();
            var key = layerKeyPrefix;

            // Same check as the one in StorageContainerVisualsSystem.
            if (!TryComp<SpriteComponent>(ent, out var sprite) || sprite.BaseRSI?.TryGetState(key, out _) != true)
                return null;

            layer.State = key;

            return (key, layer);
        }

        return null;
    }
}
