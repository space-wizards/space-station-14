using Content.Shared.Chat.TypingIndicator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory;
using Content.Client.Overlays;
using Content.Shared.Overlays;
using System.Numerics;


namespace Content.Client.Chat.TypingIndicator;

public sealed class TypingIndicatorVisualizerSystem : VisualizerSystem<TypingIndicatorComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ShowHealthIconsSystem _showHealthIcons = default!;

    protected override void OnAppearanceChange(EntityUid uid, TypingIndicatorComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var currentTypingIndicator = component.TypingIndicatorPrototype;

        var evt = new BeforeShowTypingIndicatorEvent();

        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
            _inventory.RelayEvent((uid, inventoryComp), ref evt);

        var overrideIndicator = evt.GetMostRecentIndicator();

        if (overrideIndicator != null)
            currentTypingIndicator = overrideIndicator.Value;

        if (!_prototypeManager.TryIndex(currentTypingIndicator, out var proto))
        {
            Log.Error($"Unknown typing indicator id: {component.TypingIndicatorPrototype}");
            return;
        }

        AppearanceSystem.TryGetData<bool>(uid, TypingIndicatorVisuals.IsTyping, out var isTyping, args.Component);
        var layerExists = args.Sprite.LayerMapTryGet(TypingIndicatorLayers.Base, out var layer);
        if (!layerExists)
            layer = args.Sprite.LayerMapReserveBlank(TypingIndicatorLayers.Base);

        if(GetShowHealthIconsComponent(uid, out var showHealthIconsComp) && showHealthIconsComp != null)
            _showHealthIcons.UpdateComponentVisibility(showHealthIconsComp, !isTyping);

        args.Sprite.LayerSetRSI(layer, proto.SpritePath);
        args.Sprite.LayerSetState(layer, proto.TypingState);
        args.Sprite.LayerSetShader(layer, proto.Shader);
        args.Sprite.LayerSetOffset(layer, proto.Offset);
        args.Sprite.LayerSetVisible(layer, isTyping);
    }

    // Utility method to get the ShowHealthIconsComponent from an entity or any of its items
    // StatusIconSystem has a very similar method I need to pick one place and make it shared
    private bool GetShowHealthIconsComponent(EntityUid uid, out ShowHealthIconsComponent? result)
    {
        if (TryComp<ShowHealthIconsComponent>(uid, out var showHealthIconsComp))
        {
            result = showHealthIconsComp;
            return true;
        }

        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
        {
            foreach (var slot in inventoryComp.Slots)
            {
                if (slot == null)
                    continue;

                if (_inventory.TryGetSlotEntity(uid, slot.Name, out var slotEntity))
                {
                    if(TryComp<ShowHealthIconsComponent>(slotEntity, out var slotComponent) && slotComponent != null)
                    {
                        result = slotComponent;
                        return true;
                    }
                }
            }
        }
        result = null;
        return false;
    }
}
