using Content.Server.Mind;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.RemoteControl;
using Content.Shared.RemoteControl.Components;
using Content.Shared.SSDIndicator;

namespace Content.Server.RemoteControl;

/// <summary>
/// guh
/// </summary>
public class RemoteControlSystem : SharedRemoteControlSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemotelyControllableComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<RemotelyControllableComponent, RCReturnToBodyEvent>(OnReturnToBody);

        SubscribeLocalEvent<RCRemoteComponent, UseInHandEvent>(OnUseInHand);

    }

    private void OnAfterInteractUsing(EntityUid uid, RemotelyControllableComponent comp, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<RCRemoteComponent>(args.Used, out var RemoteComp))
            return;

        if (!TryComp<RemotelyControllableComponent>(args.Target, out var controllable))
            return;

        if (comp.BoundRemote != null)
            return;


        RemoteComp.BoundTo = args.Target;
        comp.BoundRemote = args.Used;

        _popup.PopupEntity("Bound to entity.", args.User, args.User, PopupType.Medium);

        args.Handled = true;
    }

    private void OnUseInHand(EntityUid uid, RCRemoteComponent comp, ref UseInHandEvent args)
    {
        if (comp.BoundTo is null)
        {
            _popup.PopupEntity("Not bound to entity.", args.User, args.User, PopupType.Medium);
            return;
        }

        if(!TryComp<RemotelyControllableComponent>(comp.BoundTo.Value, out var controllable))
        {
            return;
        }

        if (TryComp<SSDIndicatorComponent>(args.User, out var SSD))
        {
            SSD.showSSDIcon = false;
        }

        controllable.Controller = args.User;
        controllable.IsControlled = true;

        if(_mind.TryGetMind(args.User, out var mindId, out var mind))
            _mind.Visit(mindId, comp.BoundTo.Value, mind);

        args.Handled = true;
    }

    private void OnReturnToBody(Entity<RemotelyControllableComponent> ent, ref RCReturnToBodyEvent args)
    {
        if (!TryComp<RemotelyControllableComponent>(ent.Owner, out var comp))
            return;

        if (!TryComp<VisitingMindComponent>(ent.Owner, out var visiting))
            return;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        if (TryComp<SSDIndicatorComponent>(ent.Comp.Controller, out var SSD))
            SSD.showSSDIcon = true;

        ent.Comp.Controller = null;
        _mind.UnVisit(mindId, mind);

    }
}
