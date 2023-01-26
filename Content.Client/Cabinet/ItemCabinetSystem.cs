using Content.Shared.Cabinet;
using Robust.Client.GameObjects;

namespace Content.Client.Cabinet;

public sealed class ItemCabinetSystem : VisualizerSystem<ItemCabinetVisualsComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, ItemCabinetVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData(uid, ItemCabinetVisuals.IsOpen, out bool isOpen, args.Component)
            && _appearance.TryGetData(uid, ItemCabinetVisuals.ContainsItem, out bool contains, args.Component))
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
