using Content.Shared.Containers.ItemSlots;
using Robust.Client.GameObjects;
using Content.Shared.Ganimed.Components;

namespace Content.Client.Ganimed.BookTerminal.Visualizers;

public class BookTerminalVisualizerSystem : VisualizerSystem<BookTerminalVisualsComponent>
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
	
	protected override void OnAppearanceChange(EntityUid uid, BookTerminalVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !EntityManager.TryGetComponent<ItemSlotsComponent>(uid, out var slotComp))
            return;

        if (args.Sprite.LayerMapTryGet(BookTerminalVisualLayers.Slotted, out var layer))
        {
            args.Sprite.LayerSetVisible(layer, (_itemSlotsSystem.GetItemOrNull(uid, "bookSlot") is not null));
        }
    }
}
