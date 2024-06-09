using Content.Server.Actions;
using Content.Shared.Movement.Components;
using BackflipActionEvent = Content.Shared.Movement.Components.BackflipActionEvent;
using CanBackflipComponent = Content.Shared.Movement.Components.CanBackflipComponent;

namespace Content.Server.Movement.Systems;

public sealed class BackflipSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanBackflipComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<CanBackflipComponent, BackflipActionEvent>(OnBackflipAction);
    }

    private void OnInit(EntityUid uid, CanBackflipComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.BackflipActionEntity, component.BackflipAction, uid);
    }

    public void OnBackflipAction(EntityUid uid, CanBackflipComponent comp, BackflipActionEvent args)
    {
        RaiseNetworkEvent(new DoABackFlipEvent(GetNetEntity(uid), comp.ClappaSfx));

        args.Handled = true;
    }
}
