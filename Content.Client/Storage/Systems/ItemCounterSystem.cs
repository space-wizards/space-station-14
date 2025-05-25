using Content.Shared.Rounding;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client.Storage.Systems;

public sealed class ItemCounterSystem : SharedItemCounterSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemCounterComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, ItemCounterComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || comp.LayerStates.Count < 1)
            return;

        // Skip processing if no actual
        if (!_appearanceSystem.TryGetData<int>(uid, StackVisuals.Actual, out var actual, args.Component))
            return;

        if (!_appearanceSystem.TryGetData<int>(uid, StackVisuals.MaxCount, out var maxCount, args.Component))
            maxCount = comp.LayerStates.Count;

        if (!_appearanceSystem.TryGetData<bool>(uid, StackVisuals.Hide, out var hidden, args.Component))
            hidden = false;

        if (comp.IsComposite)
            ProcessCompositeSprite(uid, actual, maxCount, comp.LayerStates, hidden, sprite: args.Sprite);
        else
            ProcessOpaqueSprite(uid, comp.BaseLayer, actual, maxCount, comp.LayerStates, hidden, sprite: args.Sprite);
    }

    public void ProcessOpaqueSprite(EntityUid uid, string layer, int count, int maxCount, List<string> states, bool hide = false, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite)
        || !_sprite.LayerMapTryGet((uid, sprite), layer, out var layerKey, logMissing: true))
            return;

        var activeState = ContentHelpers.RoundToEqualLevels(count, maxCount, states.Count);
        _sprite.LayerSetRsiState((uid, sprite), layerKey, states[activeState]);
        _sprite.LayerSetVisible((uid, sprite), layerKey, !hide);
    }

    public void ProcessCompositeSprite(EntityUid uid, int count, int maxCount, List<string> layers, bool hide = false, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        var activeTill = ContentHelpers.RoundToNearestLevels(count, maxCount, layers.Count);
        for (var i = 0; i < layers.Count; ++i)
        {
            _sprite.LayerSetVisible((uid, sprite), layers[i], !hide && i < activeTill);
        }
    }

    protected override int? GetCount(ContainerModifiedMessage msg, ItemCounterComponent itemCounter)
    {
        if (_appearanceSystem.TryGetData<int>(msg.Container.Owner, StackVisuals.Actual, out var actual))
            return actual;
        return null;
    }
}
