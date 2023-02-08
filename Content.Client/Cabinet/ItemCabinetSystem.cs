using Content.Shared.Cabinet;
using Robust.Client.GameObjects;

namespace Content.Client.Cabinet;

public sealed class ItemCabinetSystem : VisualizerSystem<ItemCabinetVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ItemCabinetVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, ItemCabinetVisuals.IsOpen, out var isOpen, args.Component)
            && AppearanceSystem.TryGetData<bool>(uid, ItemCabinetVisuals.ContainsItem, out var contains, args.Component))
        {
            var state = isOpen ? component.OpenState : component.ClosedState;
            args.Sprite.LayerSetState(ItemCabinetVisualLayers.Door, state);
            args.Sprite.LayerSetVisible(ItemCabinetVisualLayers.ContainsItem, contains);
        }
    }
}

public enum ItemCabinetVisualLayers
{
    Door,
    ContainsItem
}
