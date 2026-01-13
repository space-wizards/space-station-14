using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Client.Kitchen.EntitySystems;

public sealed class ReagentGrinderSystem : SharedReagentGrinderSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReagentGrinderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ReagentGrinderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    public override void UpdateUi(EntityUid uid)
    {
        Log.Debug($"{_timing.CurTick} UpdateUi ApplyingState: {_timing.ApplyingState} FirstTimePredicted:{_timing.IsFirstTimePredicted} InPrediction {_timing.InPrediction}");
        if (_userInterface.TryGetOpenUi(uid, ReagentGrinderUiKey.Key, out var bui))
            bui.Update();
    }
}
