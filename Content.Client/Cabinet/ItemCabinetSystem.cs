using Content.Shared.Cabinet;
using Robust.Client.GameObjects;

namespace Content.Client.Cabinet;

public sealed class ItemCabinetSystem : SharedItemCabinetSystem
{
    protected override void UpdateAppearance(EntityUid uid, ItemCabinetComponent? cabinet = null)
    {
        if (!Resolve(uid, ref cabinet))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var state = cabinet.Opened ? cabinet.OpenState : cabinet.ClosedState;
        if (state != null)
            sprite.LayerSetState(ItemCabinetVisualLayers.Door, state);
        sprite.LayerSetVisible(ItemCabinetVisualLayers.ContainsItem, cabinet.CabinetSlot.HasItem);
    }
}

public enum ItemCabinetVisualLayers
{
    Door,
    ContainsItem
}
