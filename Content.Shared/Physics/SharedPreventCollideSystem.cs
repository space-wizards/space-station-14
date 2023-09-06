using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Physics;

public sealed class SharedPreventCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PreventCollideComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PreventCollideComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<PreventCollideComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnGetState(EntityUid uid, PreventCollideComponent component, ref ComponentGetState args)
    {
        args.State = new PreventCollideComponentState(GetNetEntity(component.Uid));
    }

    private void OnHandleState(EntityUid uid, PreventCollideComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PreventCollideComponentState state)
            return;

        component.Uid = EnsureEntity<PreventCollideComponent>(state.Uid, uid);
    }

    private void OnPreventCollide(EntityUid uid, PreventCollideComponent component, ref PreventCollideEvent args)
    {
        if (component.Uid == args.OtherEntity)
            args.Cancelled = true;
    }

}
