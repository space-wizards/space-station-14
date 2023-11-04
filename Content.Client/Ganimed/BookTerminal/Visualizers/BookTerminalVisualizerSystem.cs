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

        if (args.Sprite.LayerMapTryGet(BookTerminalVisualLayers.Slotted, out var slotLayer))
        {
            args.Sprite.LayerSetVisible(slotLayer, (_itemSlotsSystem.GetItemOrNull(uid, "cartridgeSlot") is not null));
        }
		
		var cartridge = _itemSlotsSystem.GetItemOrNull(uid, "cartridgeSlot");
		
		if (args.Sprite.LayerMapTryGet(BookTerminalVisualLayers.Full, out var fullLayer))
		{
			args.Sprite.LayerSetVisible(fullLayer, false);
			if (cartridge is not null && EntityManager.TryGetComponent<BookTerminalCartridgeComponent>(cartridge, out BookTerminalCartridgeComponent? cartridgeComp))
				args.Sprite.LayerSetVisible(fullLayer!, cartridgeComp.CurrentCharge == cartridgeComp.FullCharge);
		}
			
		if (args.Sprite.LayerMapTryGet(BookTerminalVisualLayers.High, out var highLayer))
		{
			args.Sprite.LayerSetVisible(highLayer, false);
			if (cartridge is not null && EntityManager.TryGetComponent<BookTerminalCartridgeComponent>(cartridge, out BookTerminalCartridgeComponent? cartridgeComp))
				args.Sprite.LayerSetVisible(highLayer, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 1.43f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge);
		}
			
		if (args.Sprite.LayerMapTryGet(BookTerminalVisualLayers.Medium, out var mediumLayer))
		{
			args.Sprite.LayerSetVisible(mediumLayer, false);
			if (cartridge is not null && EntityManager.TryGetComponent<BookTerminalCartridgeComponent>(cartridge, out BookTerminalCartridgeComponent? cartridgeComp))
				args.Sprite.LayerSetVisible(mediumLayer, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 2.5f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 1.43f);
		}
			
		if (args.Sprite.LayerMapTryGet(BookTerminalVisualLayers.Low, out var lowLayer))
		{
			args.Sprite.LayerSetVisible(lowLayer, false);
			if (cartridge is not null && EntityManager.TryGetComponent<BookTerminalCartridgeComponent>(cartridge, out BookTerminalCartridgeComponent? cartridgeComp))
				args.Sprite.LayerSetVisible(lowLayer, cartridgeComp.CurrentCharge > 0 && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 2.5f);
		}
			
		if (args.Sprite.LayerMapTryGet(BookTerminalVisualLayers.None, out var noneLayer))
		{
			args.Sprite.LayerSetVisible(noneLayer, false);
			if (cartridge is not null && EntityManager.TryGetComponent<BookTerminalCartridgeComponent>(cartridge, out BookTerminalCartridgeComponent? cartridgeComp))
				args.Sprite.LayerSetVisible(noneLayer, cartridgeComp.CurrentCharge < 1);
		}
    }
}
