using Content.Shared.Chat.TypingIndicator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory;

namespace Content.Client.Chat.TypingIndicator;

public sealed class TypingIndicatorVisualizerSystem : VisualizerSystem<TypingIndicatorComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;


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

        args.Sprite.LayerSetRSI(layer, proto.SpritePath);
        args.Sprite.LayerSetState(layer, proto.TypingState);
        args.Sprite.LayerSetShader(layer, proto.Shader);
        args.Sprite.LayerSetOffset(layer, proto.Offset);
        args.Sprite.LayerSetVisible(layer, isTyping);
    }
}
