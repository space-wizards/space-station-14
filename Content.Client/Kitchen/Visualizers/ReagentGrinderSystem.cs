using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Kitchen.Visualizers;

public sealed class ReagentGrinderSystem : SharedReagentGrinderSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReagentGrinderComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, ReagentGrinderComponent component, ref AppearanceChangeEvent args)
    {
        args.Component.TryGetData(ReagentGrinderVisualState.BeakerAttached, out bool hasBeaker);
        var state = hasBeaker ? component.BeakerState : component.EmptyState;
        args.Sprite?.LayerSetState(ReagentGrinderVisualLayers.Base, state);
    }
}

public enum ReagentGrinderVisualLayers : byte
{
    Base
}

