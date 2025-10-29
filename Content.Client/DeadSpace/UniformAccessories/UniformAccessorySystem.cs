using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DeadSpace.UniformAccessories;
using Content.Shared.DeadSpace.UniformAccessories.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;

namespace Content.Client.DeadSpace.UniformAccessories;

public sealed class UniformAccessorySystem : SharedUniformAccessorySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    private readonly Dictionary<EntityUid, string> _layerKeyCache = new();
    [Dependency] private readonly IPlayerManager _player = default!;

    public event Action? PlayerAccessoryVisualsUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UniformAccessoryHolderComponent, GetEquipmentVisualsEvent>(OnHolderGetEquipmentVisuals,
            after: [typeof(ClothingSystem)]);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, AfterAutoHandleStateEvent>(OnHolderVisualUpdate);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EntInsertedIntoContainerMessage>(OnHolderVisualUpdate);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EntRemovedFromContainerMessage>(OnHolderRemovedContainer);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EquipmentVisualsUpdatedEvent>(OnHolderVisualsUpdated,
            after: [typeof(ClothingSystem)]);
    }

    private void OnHolderGetEquipmentVisuals(Entity<UniformAccessoryHolderComponent> ent,
        ref GetEquipmentVisualsEvent args)
    {
        if (TryComp(_player.LocalEntity, out HumanoidAppearanceComponent? humanoid) && ShouldHideAccessories(humanoid))
            return;

        var clothingSprite = CompOrNull<SpriteComponent>(ent);
        if (!_container.TryGetContainer(ent, UniformAccessoryHolderComponent.ContainerId, out var container))
            return;

        var index = 0;
        foreach (var accessory in container.ContainedEntities)
        {
            if (!TryComp<UniformAccessoryComponent>(accessory, out var accessoryComp))
                continue;

            var layerKey = GetLayerKey(accessory, accessoryComp, index);

            if (accessoryComp.PlayerSprite is { } specified)
            {
                if (clothingSprite != null && accessoryComp.HasIconSprite)
                {
                    var li = clothingSprite.LayerMapReserveBlank(layerKey);
                    clothingSprite.LayerSetVisible(li, !accessoryComp.Hidden);
                    clothingSprite.LayerSetRSI(li, specified.RsiPath);
                    clothingSprite.LayerSetState(li, specified.RsiState);
                }

                if (args.Layers.All(t => t.Item1 != layerKey))
                {
                    args.Layers.Add((layerKey, new PrototypeLayerData
                    {
                        RsiPath = specified.RsiPath.ToString(),
                        State = specified.RsiState,
                        Visible = !accessoryComp.Hidden,
                    }));
                }

                index++;
                continue;
            }

            var accessorySlot = GetAccessorySlot(accessory) ?? args.Slot;
            var accessoryEv = new GetEquipmentVisualsEvent(args.Equipee, accessorySlot);
            ForceAccessoryRSI(accessory, accessoryEv, layerKey);
            RaiseLocalEvent(accessory, accessoryEv);

            if (accessoryEv.Layers.Count > 0)
            {
                var layerData = accessoryEv.Layers[0].Item2;
                if (clothingSprite != null && accessoryComp.HasIconSprite)
                {
                    var li = clothingSprite.LayerMapReserveBlank(layerKey);
                    clothingSprite.LayerSetVisible(li, !accessoryComp.Hidden);
                    if (layerData.RsiPath != null)
                        clothingSprite.LayerSetRSI(li, layerData.RsiPath);
                    if (layerData.State != null)
                        clothingSprite.LayerSetState(li, layerData.State);
                }

                if (args.Layers.All(t => t.Item1 != layerKey))
                {
                    args.Layers.Add((layerKey, new PrototypeLayerData
                    {
                        RsiPath = layerData.RsiPath,
                        State = layerData.State,
                        TexturePath = layerData.TexturePath,
                        Color = layerData.Color,
                        Scale = layerData.Scale,
                        Visible = !accessoryComp.Hidden && (layerData.Visible ?? true),
                    }));
                }
            }

            index++;
        }

        PlayerAccessoryVisualsUpdated?.Invoke();
    }

    private string? GetAccessorySlot(EntityUid uid)
    {
        if (TryComp<ClothingComponent>(uid, out var clothing))
        {
            if (!string.IsNullOrEmpty(clothing.InSlot))
                return clothing.InSlot;

            foreach (var slot in Enum.GetValues<SlotFlags>())
            {
                if (slot == SlotFlags.NONE)
                    continue;
                if ((clothing.Slots & slot) != 0)
                    return slot.ToString().ToLowerInvariant();
            }
        }

        if (TryComp<UniformAccessoryComponent>(uid, out var accComp) && !string.IsNullOrEmpty(accComp.Category))
            return accComp.Category.ToLowerInvariant();

        return null;
    }

    private void ForceAccessoryRSI(EntityUid accessory, GetEquipmentVisualsEvent ev, string layerKey)
    {
        var (rsiPath, state) = GetAccessorySpriteInfo(accessory);
        if (string.IsNullOrEmpty(rsiPath))
            return;

        ev.Layers.Clear();
        var layerData = new PrototypeLayerData
        {
            RsiPath = rsiPath,
            Visible = true,
        };

        var clothingVisualsEv = new GetEquipmentVisualsEvent(ev.Equipee, ev.Slot);
        RaiseLocalEvent(accessory, clothingVisualsEv);

        if (clothingVisualsEv.Layers.Count > 0)
            layerData.State = clothingVisualsEv.Layers[0].Item2.State;
        else if (TryComp<ClothingComponent>(accessory, out var clothing))
        {
            layerData.State = !string.IsNullOrEmpty(clothing.EquippedState)
                ? clothing.EquippedState
                : !string.IsNullOrEmpty(clothing.EquippedPrefix)
                    ? $"{clothing.EquippedPrefix}-equipped-{ev.Slot.ToUpperInvariant()}"
                    : $"equipped-{ev.Slot.ToUpperInvariant()}";
        }
        else
            layerData.State = $"equipped-{ev.Slot.ToUpperInvariant()}";

        ev.Layers.Add((layerKey, layerData));
    }

    private (string? RsiPath, string? State) GetAccessorySpriteInfo(EntityUid uid)
    {
        if (TryComp<ClothingComponent>(uid, out var clothing) && !string.IsNullOrEmpty(clothing.RsiPath))
            return (clothing.RsiPath, clothing.EquippedState);
        if (TryComp<SpriteComponent>(uid, out var sprite) && sprite.BaseRSI != null)
            return (sprite.BaseRSI.Path.ToString(), null);
        return (null, null);
    }

    private void OnHolderVisualUpdate(Entity<UniformAccessoryHolderComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {
        _item.VisualsChanged(ent);
        if (TryComp<UniformAccessoryComponent>(args.Entity, out var acc) && acc.DrawOnItemIcon)
            UpdateItemIconOverlay(ent.Owner, args.Entity, true);
    }

    private void OnHolderVisualUpdate(Entity<UniformAccessoryHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnHolderRemovedContainer(Entity<UniformAccessoryHolderComponent> ent,
        ref EntRemovedFromContainerMessage args)
    {
        var item = args.Entity;
        if (!TryComp<UniformAccessoryComponent>(item, out var accessoryComp))
            return;

        var index = 0;
        foreach (var accessory in args.Container.ContainedEntities)
        {
            if (accessory == item)
                break;
            index++;
        }

        var layerKey = GetLayerKey(item, accessoryComp, index);
        if (TryComp(ent.Owner, out SpriteComponent? clothingSprite) &&
            clothingSprite.LayerMapTryGet(layerKey, out var clothingLayer))
            clothingSprite.LayerSetVisible(clothingLayer, false);

        _item.VisualsChanged(ent);
        if (accessoryComp.DrawOnItemIcon)
            UpdateItemIconOverlay(ent.Owner, args.Entity, false);

        _layerKeyCache.Remove(item);
    }

    private void UpdateItemIconOverlay(EntityUid holder, EntityUid accessory, bool add)
    {
        if (!TryComp<SpriteComponent>(holder, out var itemSprite))
            return;

        var key = $"AccessoryIcon_{accessory}";
        if (!TryComp<UniformAccessoryComponent>(accessory, out var acc) || !acc.DrawOnItemIcon)
            return;

        if (add)
        {
            var layer = itemSprite.LayerMapReserveBlank(key);
            if (acc.PlayerSprite is { } specified)
            {
                itemSprite.LayerSetRSI(layer, specified.RsiPath.ToString());
                itemSprite.LayerSetState(layer, specified.RsiState ?? "icon");
                itemSprite.LayerSetVisible(layer, !acc.Hidden);
            }
            else
            {
                var accessorySlot = GetAccessorySlot(accessory) ?? "outerClothing";
                var ev = new GetEquipmentVisualsEvent(holder, accessorySlot);
                ForceAccessoryRSI(accessory, ev, key);
                if (ev.Layers.Count > 0)
                {
                    var layerData = ev.Layers[0].Item2;
                    if (layerData.RsiPath != null)
                        itemSprite.LayerSetRSI(layer, layerData.RsiPath);
                    if (layerData.State != null)
                        itemSprite.LayerSetState(layer, layerData.State);
                    itemSprite.LayerSetVisible(layer, !acc.Hidden);
                }
            }
        }
        else
        {
            if (itemSprite.LayerMapTryGet(key, out var layer))
                itemSprite.RemoveLayer(layer);
        }
    }

    private void OnHolderVisualsUpdated(Entity<UniformAccessoryHolderComponent> ent,
        ref EquipmentVisualsUpdatedEvent args)
    {
        if (!_container.TryGetContainer(ent, UniformAccessoryHolderComponent.ContainerId, out var container))
            return;

        if (!TryComp(args.Equipee, out SpriteComponent? sprite))
            return;

        var index = 0;
        foreach (var accessory in container.ContainedEntities)
        {
            if (!TryComp<UniformAccessoryComponent>(accessory, out var acc))
                continue;

            var key = GetLayerKey(accessory, acc, index);
            if (!args.RevealedLayers.Contains(key))
                continue;

            if (!sprite.LayerMapTryGet(key, out var layer) ||
                !sprite.TryGetLayer(layer, out var layerData))
                continue;

            var data = layerData.ToPrototypeData();
            sprite.RemoveLayer(layer);
            layer = sprite.LayerMapReserveBlank(key);
            sprite.LayerSetData(layer, data);
            index++;
        }
    }

    private bool ShouldHideAccessories(HumanoidAppearanceComponent humanoid)
    {
        return false;
    }

    private string GetLayerKey(EntityUid uid, UniformAccessoryComponent component, int index)
    {
        if (_layerKeyCache.TryGetValue(uid, out var cachedKey))
            return cachedKey;

        var key = !string.IsNullOrEmpty(component.LayerKey) ? component.LayerKey : $"Accessory_{uid.Id}_{index}";
        _layerKeyCache[uid] = key;
        return key;
    }
}
