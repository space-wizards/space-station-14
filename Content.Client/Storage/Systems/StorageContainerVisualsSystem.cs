using Content.Client.Items.Systems;
using Content.Client.Storage.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Rounding;
using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

/// <inheritdoc cref="StorageContainerVisualsComponent"/>
public sealed partial class StorageContainerVisualsSystem : VisualizerSystem<StorageContainerVisualsComponent>
{
    [Dependency] private ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Have these systems go first & add their visuals, then after that, we add our own. No more conflicting visuals!
        SubscribeLocalEvent<StorageContainerVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) });
        SubscribeLocalEvent<StorageContainerVisualsComponent, GetEquipmentVisualsEvent>(OnGetClothingVisuals, after: new[] { typeof(ClothingSystem) });
    }

    protected override void OnAppearanceChange(EntityUid uid, StorageContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.StorageUsed, out var used, args.Component))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.Capacity, out var capacity, args.Component))
            return;

        var fraction = used / (float)capacity;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.FillLayer, out var fillLayer, false))
            return;

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, component.MaxFillLevels + 1);

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

    private void OnGetHeldVisuals(Entity<StorageContainerVisualsComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (ent.Comp.InHandsFillBaseName == null)
            return;

        if (!TryComp<ItemComponent>(ent, out var item))
            return;

        var inhandPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand-";
        var layerKeyPrefix = inhandPrefix + args.Location.ToString().ToLowerInvariant() + ent.Comp.InHandsFillBaseName;

        if (GetVisualsLayer(ent, layerKeyPrefix, ent.Comp.InHandsMaxFillLevels) is not { } layer)
            return;

        args.Layers.Add(layer);
    }

    private void OnGetClothingVisuals(Entity<StorageContainerVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.EquippedFillBaseName == null)
            return;

        if (!TryComp<ClothingComponent>(ent, out var clothing))
            return;

        var equippedPrefix = clothing.EquippedPrefix == null ? $"equipped-{args.Slot}" : $"{clothing.EquippedPrefix}-equipped-{args.Slot}";
        var layerKeyPrefix = equippedPrefix + ent.Comp.EquippedFillBaseName;

        if (GetVisualsLayer(ent, layerKeyPrefix, ent.Comp.InHandsMaxFillLevels) is not { } layer)
            return;

        args.Layers.Add(layer);
    }

    private (string Key, PrototypeLayerData Layer)? GetVisualsLayer(Entity<StorageContainerVisualsComponent> ent, string layerKeyPrefix, int maxFillLevels)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance)
            || !AppearanceSystem.TryGetData<int>(ent, StorageVisuals.StorageUsed, out var used, appearance)
            || !AppearanceSystem.TryGetData<int>(ent, StorageVisuals.Capacity, out var capacity, appearance))
            return null;

        var fraction = used / (float)capacity;

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, maxFillLevels + 1);
        if (closestFillSprite <= 0)
            return null;

        var layer = new PrototypeLayerData();
        var key = layerKeyPrefix + closestFillSprite;

        // Same check as the one in SolutionContainerVisualsSystem.
        if (!TryComp<SpriteComponent>(ent, out var sprite) || sprite.BaseRSI?.TryGetState(key, out _) != true)
            return null;

        layer.State = key;

        return (key, layer);
    }
}
