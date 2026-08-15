using Content.Shared.Rounding;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

namespace Content.Client.Storage.Systems;

public sealed partial class ItemCounterSystem : SharedItemCounterSystem
{
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(EntityUid uid, ItemCounterComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || comp.LayerStates.Count < 1)
            return;

        // Skip processing if no actual
        if (!args.TryGetData<int>(StackVisuals.Actual, out var actual))
            return;

        if (!args.TryGetData<int>(StackVisuals.MaxCount, out var maxCount))
            maxCount = comp.LayerStates.Count;

        if (!args.TryGetData<bool>(StackVisuals.Hide, out var hidden))
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
