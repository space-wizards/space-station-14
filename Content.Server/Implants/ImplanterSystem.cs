using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Server.Implants;

public sealed partial class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeImplanted();

        SubscribeLocalEvent<ImplanterComponent, HandDeselectedEvent>(OnHandDeselect);
        SubscribeLocalEvent<ImplanterComponent, AfterInteractEvent>(OnImplanterAfterInteract);
        SubscribeLocalEvent<ImplanterComponent, ComponentGetState>(OnImplanterGetState);

        SubscribeLocalEvent<ImplanterComponent, DoAfterEvent<ImplantEvent>>(OnImplant);
        SubscribeLocalEvent<ImplanterComponent, DoAfterEvent<DrawEvent>>(OnDraw);
    }

    private void OnImplanterAfterInteract(EntityUid uid, ImplanterComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || args.Handled)
            return;

        //Simplemobs and regular mobs should be injectable, but only regular mobs have mind.
        //So just don't implant/draw anything that isn't living or is a guardian
        //TODO: Rework a bit when surgery is in to work with implant cases
        if (!HasComp<MobStateComponent>(args.Target.Value) || HasComp<GuardianComponent>(args.Target.Value))
            return;

        //TODO: Rework when surgery is in for implant cases
        if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly)
        {
            TryDraw(component, args.User, args.Target.Value, uid);
        }
        else
        {
            //Implant self instantly, otherwise try to inject the target.
            if (args.User == args.Target)
                Implant(uid, args.Target.Value, component);

            else
                TryImplant(component, args.User, args.Target.Value, uid);
        }
        args.Handled = true;
    }

    private void OnHandDeselect(EntityUid uid, ImplanterComponent component, HandDeselectedEvent args)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    /// <summary>
    /// Attempt to implant someone else.
    /// </summary>
    /// <param name="component">Implanter component</param>
    /// <param name="user">The entity using the implanter</param>
    /// <param name="target">The entity being implanted</param>
    /// <param name="implanter">The implanter being used</param>
    public void TryImplant(ImplanterComponent component, EntityUid user, EntityUid target, EntityUid implanter)
    {
        if (component.CancelToken != null)
            return;

        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        var userName = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), user, target, PopupType.LargeCaution);

        component.CancelToken?.Cancel();
        component.CancelToken = new CancellationTokenSource();

        var implantEvent = new ImplantEvent();

        _doAfter.DoAfter(new DoAfterEventArgs(user, component.ImplantTime, component.CancelToken.Token,target:target, used:implanter)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = true
        }, implantEvent);
    }

    /// <summary>
    /// Try to remove an implant and store it in an implanter
    /// </summary>
    /// <param name="component">Implanter component</param>
    /// <param name="user">The entity using the implanter</param>
    /// <param name="target">The entity getting their implant removed</param>
    /// <param name="implanter">The implanter being used</param>
    //TODO: Remove when surgery is in
    public void TryDraw(ImplanterComponent component, EntityUid user, EntityUid target, EntityUid implanter)
    {
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        component.CancelToken?.Cancel();
        component.CancelToken = new CancellationTokenSource();

        var drawEvent = new DrawEvent();

        _doAfter.DoAfter(new DoAfterEventArgs(user, component.DrawTime, target:target,used:implanter)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = true
        }, drawEvent);
    }

    private void OnImplanterGetState(EntityUid uid, ImplanterComponent component, ref ComponentGetState args)
    {
        args.State = new ImplanterComponentState(component.CurrentMode, component.ImplantOnly);
    }

    private void OnImplant(EntityUid uid, ImplanterComponent component, DoAfterEvent<ImplantEvent> args)
    {
        if (args.Cancelled)
        {
            component.CancelToken = null;
            return;
        }

        if (args.Handled || args.Args.Target == null || args.Args.Used == null)
            return;

        Implant(args.Args.Used.Value, args.Args.Target.Value, component);

        args.Handled = true;
        component.CancelToken = null;
    }

    private void OnDraw(EntityUid uid, ImplanterComponent component, DoAfterEvent<DrawEvent> args)
    {
        if (args.Cancelled)
        {
            component.CancelToken = null;
            return;
        }

        if (args.Handled || args.Args.Used == null || args.Args.Target == null)
            return;

        Draw(args.Args.Used.Value, args.Args.User, args.Args.Target.Value, component);

        args.Handled = true;
        component.CancelToken = null;
    }

    private sealed class ImplantEvent : EntityEventArgs
    {

    }

    private sealed class DrawEvent : EntityEventArgs
    {

    }
}
