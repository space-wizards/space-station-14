using System.Linq;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Containers.ItemSlot;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

public sealed partial class ItemSlotVisualsSystem : VisualizerSystem<ItemSlotVisualsComponent>
{
    [Dependency] private ItemSystem _itemSystem = default!;

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

    // Have these systems go first & add their visuals, then after that, we add our own. No more conflicting visuals!
    [SubscribeLocalEvent(after: [typeof(ItemSystem)])]
    private void OnGetHeldVisuals(Entity<ItemSlotVisualsComponent> ent, ref GetInhandVisualsEvent args)
    {
        foreach (var visual in ent.Comp.SlotVisuals.Values)
        {
            if (!TryComp<AppearanceComponent>(ent, out var appearance)
                || !AppearanceSystem.TryGetData(ent, visual.Layer, out bool hasItem, appearance)
                || !hasItem)
                continue;

            if (!TryComp<ItemComponent>(ent, out _))
                return;

            if (!visual.InhandVisuals.TryGetValue(args.Location, out var layers))
                return;

            var i = 0;
            var defaultKey = $"inhand-{args.Location.ToString().ToLowerInvariant()}-fill-{visual.Layer}";
            foreach (var layer in layers)
            {
                var key = layer.MapKeys?.FirstOrDefault();
                if (key == null)
                {
                    key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                    i++;
                }

                args.Layers.Add((key, layer));
            }
        }
    }

    [SubscribeLocalEvent(after: [typeof(ClothingSystem)])]
    private void OnGetClothingVisuals(Entity<ItemSlotVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        foreach (var visual in ent.Comp.SlotVisuals.Values)
        {
            if (!TryComp(ent, out AppearanceComponent? appearance)
                || !AppearanceSystem.TryGetData(ent, visual.Layer, out bool hasItem, appearance)
                || !hasItem)
                continue;

            if (!TryComp<ClothingComponent>(ent, out _))
                return;

            if (!TryComp(args.Equipee, out InventoryComponent? inventory))
                return;

            List<PrototypeLayerData>? layers = null;

            // attempt to get species specific data
            if (inventory.SpeciesId != null)
                visual.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

            // No species specific data. Try to default to generic data.
            if (layers == null && !visual.ClothingVisuals.TryGetValue(args.Slot, out layers))
                return;

            var i = 0;
            var defaultKey = $"{args.Slot}-fill-{visual.Layer}";
            foreach (var layer in layers)
            {
                var key = layer.MapKeys?.FirstOrDefault();
                if (key == null)
                {
                    key = i == 0 ? defaultKey : $"{defaultKey}-{i}";
                    i++;
                }

                args.Layers.Add((key, layer));
            }
        }
    }
}
