using Content.Shared.Chat.TypingIndicator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using System.Numerics;


namespace Content.Client.Chat.TypingIndicator;

public sealed class TypingIndicatorVisualizerSystem : VisualizerSystem<TypingIndicatorComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <summary>
    ///     How far the typing indicator should be offset when the entity is using a HUD.
    /// </summary>
    private const float TypingIndicatorVerticalOffset = -0.12f;

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

        // If the entity is using eyewear that shows a HUD, offset the typing indicator slightly down so it's not obscured.
        // TODO: Consider making the typing indicator an actual overlay instead of a sprite layer.
        var verticalOffset = 0f;
        if(_inventory.TryGetSlotEntity(uid, "eyes", out var slotEntity) &&
            HasComp<ShowHealthBarsComponent>(slotEntity) && HasComp<ShowHealthIconsComponent>(slotEntity))
        {
            verticalOffset = TypingIndicatorVerticalOffset;
        }

        var offset = new Vector2(proto.Offset.X, proto.Offset.Y + verticalOffset);

        args.Sprite.LayerSetRSI(layer, proto.SpritePath);
        args.Sprite.LayerSetState(layer, proto.TypingState);
        args.Sprite.LayerSetShader(layer, proto.Shader);
        args.Sprite.LayerSetOffset(layer, offset);
        args.Sprite.LayerSetVisible(layer, isTyping);
    }
}
