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

        SubscribeLocalEvent<ImplanterComponent, DoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ImplanterComponent, ImplanterDrawCompleteEvent>(OnDrawAttemptSuccess);
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
        //TODO: Cancel logic
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
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        var userName = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), user, target, PopupType.LargeCaution);

        _doAfter.DoAfter(new DoAfterEventArgs(user, component.ImplantTime, target:target, used:implanter)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
        });
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

        _doAfter.DoAfter(new DoAfterEventArgs(user, component.DrawTime, target:target,used:implanter)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            UsedFinishedEvent = new ImplanterDrawCompleteEvent(implanter, user, target),
            UsedCancelledEvent = new ImplanterCancelledEvent()
        });
    }

    private void OnImplanterGetState(EntityUid uid, ImplanterComponent component, ref ComponentGetState args)
    {
        args.State = new ImplanterComponentState(component.CurrentMode, component.ImplantOnly);
    }

    private void OnDoAfter(EntityUid uid, ImplanterComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        Implant(args.Args.Used.Value, args.Args.Target.Value, component);

        args.Handled = true;
    }

    private void OnDrawAttemptSuccess(EntityUid uid, ImplanterComponent component, ImplanterDrawCompleteEvent args)
    {
        //TODO: Add draw logic for another doafter
        Draw(args.Implanter, args.User, args.Target, component);
    }

    private sealed class ImplanterCancelledEvent : EntityEventArgs
    {

    }

    private sealed class ImplanterDrawCompleteEvent : EntityEventArgs
    {
        public EntityUid Implanter;
        public EntityUid User;
        public EntityUid Target;

        public ImplanterDrawCompleteEvent(EntityUid implanter, EntityUid user, EntityUid target)
        {
            Implanter = implanter;
            User = user;
            Target = target;
        }
    }

}
