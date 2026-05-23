using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
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
        SubscribeLocalEvent<ItemSlotVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
        SubscribeLocalEvent<ItemSlotVisualsComponent, GetEquipmentVisualsEvent>(OnGetClothingVisuals);
    }

    protected override void OnAppearanceChange(EntityUid uid, ItemSlotVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.Layer, out var layerIndex, false))
            return;

        var filled = AppearanceSystem.TryGetData(uid, component.Layer, out bool hasItem, args.Component) && hasItem;

        if (filled && !string.IsNullOrEmpty(component.FillBaseName))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, true);
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), layerIndex, component.FillBaseName);
        }
        else
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, false);
        }

        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, ItemSlotVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp<ItemComponent>(uid, out var item))
            return;

        if (component.InHandsFillBaseName == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, component.Layer, out bool contains) || !contains)
            return;

        var heldPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand-";

        // No need for fillLevels if it'll just fit one item.
        var key = heldPrefix + args.Location.ToString().ToLowerInvariant() + component.InHandsFillBaseName;

        var layer = new PrototypeLayerData();
        layer.State = key;

        args.Layers.Add((key, layer));
    }

    private void OnGetClothingVisuals(Entity<ItemSlotVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp<ClothingComponent>(ent, out var clothing))
            return;

        if (ent.Comp.InHandsFillBaseName == null)
            return;

        if (!AppearanceSystem.TryGetData(ent, ent.Comp.Layer, out bool contains) || !contains)
            return;

        var equippedPrefix = clothing.EquippedPrefix == null ? $"equipped-{args.Slot}" : $"{clothing.EquippedPrefix}-equipped-{args.Slot}";
        var key = equippedPrefix + ent.Comp.EquippedFillBaseName;

        // Same check as the one in StorageContainerVisualsSystem.
        if (!TryComp<SpriteComponent>(ent, out var sprite) || sprite.BaseRSI == null || !sprite.BaseRSI.TryGetState(key, out _))
            return;

        var layer = new PrototypeLayerData();
        layer.State = key;

        args.Layers.Add((key, layer));
    }
}
