using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Physics;

public sealed class SharedPreventCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PreventCollideComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PreventCollideComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, PreventCollideComponent component, ref ComponentGetState args)
    {
        args.State = new PreventCollideComponentState(component);
    }

    private void OnHandleState(EntityUid uid, PreventCollideComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PreventCollideComponentState state)
            return;

        component.Uid = state.Uid;
    }

}
