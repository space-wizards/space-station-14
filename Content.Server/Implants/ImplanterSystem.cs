using System.Linq;
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Robust.Server.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, HandDeselectedEvent>(OnHandDeselect);
        SubscribeLocalEvent<ImplanterComponent, AfterInteractEvent>(OnImplanterAfterInteract);
        SubscribeLocalEvent<ImplanterComponent, ComponentGetState>(OnImplanterGetState);

        SubscribeLocalEvent<ImplanterComponent, ImplanterCompleteEvent>(OnImplantAttemptSuccess);
        SubscribeLocalEvent<ImplanterComponent, ImplanterCancelledEvent>(OnImplantAttemptFail);

    }

    private void OnImplanterAfterInteract(EntityUid uid, ImplanterComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || args.Handled)
            return;

        if (component.CancelToken != null)
        {
            args.Handled = true;
            return;
        }

        //Simplemobs and regular mobs should be injectable, but only regular mobs have mind.
        //So just don't inject anything that isn't living or is a guardian
        if (!HasComp<MobStateComponent>(args.Target.Value) || HasComp<GuardianComponent>(args.Target.Value))
            return;

        //Implant self instantly, otherwise try to inject the target.
        if (args.User == args.Target)
            Implant(uid, args.Target.Value, component);

        else
            TryImplant(component, args.User, args.Target.Value, uid);

        args.Handled = true;
    }

    private void OnHandDeselect(EntityUid uid, ImplanterComponent component, HandDeselectedEvent args)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    //Attempt to implant someone else
    public void TryImplant(ImplanterComponent component, EntityUid user, EntityUid target, EntityUid implanter)
    {
        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, Filter.Entities(user));

        var userName = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), user, Filter.Entities(target));

        component.CancelToken = new CancellationTokenSource();

        _doAfter.DoAfter(new DoAfterEventArgs(user, component.ImplantTime, component.CancelToken.Token, implanter)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            TargetFinishedEvent = new ImplanterCompleteEvent(implanter, target),
            TargetCancelledEvent = new ImplanterCancelledEvent()
        });
    }

    private void OnImplanterGetState(EntityUid uid, ImplanterComponent component, ref ComponentGetState args)
    {
        args.State = new ImplanterComponentState(component.CurrentMode);
    }

    private void OnImplantAttemptSuccess(EntityUid uid, ImplanterComponent component, ImplanterCompleteEvent args)
    {
        component.CancelToken = null;
        Implant(args.Implanter, args.Target, component);
    }

    private void OnImplantAttemptFail(EntityUid uid, ImplanterComponent component, ImplanterCancelledEvent args)
    {
        component.CancelToken = null;
    }

    private sealed class ImplanterCompleteEvent : EntityEventArgs
    {
        public EntityUid Implanter;
        public EntityUid Target;

        public ImplanterCompleteEvent(EntityUid implanter, EntityUid target)
        {
            Implanter = implanter;
            Target = target;
        }
    }

    private sealed class ImplanterCancelledEvent : EntityEventArgs
    {

    }
}
