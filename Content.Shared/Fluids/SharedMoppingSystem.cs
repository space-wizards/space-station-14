using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids;

public abstract class SharedMoppingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, ComponentGetState>(OnAbsorbentGetState);
        SubscribeLocalEvent<AbsorbentComponent, ComponentHandleState>(OnAbsorbentHandleState);
    }

    private void OnAbsorbentHandleState(EntityUid uid, AbsorbentComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AbsorbentComponentState state)
            return;

        if (component.Progress.Equals(state.Progress))
            return;

        component.Progress = state.Progress;
    }

    private void OnAbsorbentGetState(EntityUid uid, AbsorbentComponent component, ref ComponentGetState args)
    {
        args.State = new AbsorbentComponentState()
        {
            Progress = component.Progress,
        };
    }

    [Serializable, NetSerializable]
    protected sealed class AbsorbentComponentState : ComponentState
    {
        public float Progress;
    }
}
