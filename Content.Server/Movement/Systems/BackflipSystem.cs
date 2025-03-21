using Content.Server.Actions;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;

namespace Content.Server.Movement.Systems;

public sealed class BackflipSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanBackflipComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<CanBackflipComponent, BackflipActionEvent>(OnBackflipAction);
        SubscribeLocalEvent<CanBackflipComponent, InteractionFailureEvent>(OnInteractFailed);
    }

    private void OnInit(EntityUid uid, CanBackflipComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.BackflipActionEntity, component.BackflipAction, uid);
    }

    public void OnBackflipAction(EntityUid uid, CanBackflipComponent comp, BackflipActionEvent args)
    {
        RaiseNetworkEvent(new DoABackFlipEvent(GetNetEntity(uid), comp.SoundEffect));

        args.Handled = true;
    }

    public void OnInteractFailed(EntityUid uid, CanBackflipComponent comp, InteractionFailureEvent args)
    {
        if (!comp.BackflipOnFailedInteract)
        {
            return;
        }

        RaiseLocalEvent(uid, new BackflipActionEvent());
    }
}
