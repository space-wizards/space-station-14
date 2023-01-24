using System.Threading;
using Content.Server.DoAfter;
using Content.Shared.Alert;
using Content.Shared.Ensnaring.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Throwing;

namespace Content.Server.Ensnaring;

public sealed partial class EnsnareableSystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public void InitializeEnsnaring()
    {
        SubscribeLocalEvent<EnsnaringComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<EnsnaringComponent, StepTriggerAttemptEvent>(AttemptStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, StepTriggeredEvent>(OnStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnComponentRemove(EntityUid uid, EnsnaringComponent component, ComponentRemove args)
    {
        if (!TryComp<EnsnareableComponent>(component.Ensnared, out var ensnared))
            return;

        if (ensnared.IsEnsnared)
            ForceFree(component);
    }

    private void AttemptStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggeredEvent args)
    {
        TryEnsnare(args.Tripper, component);
    }

    private void OnThrowHit(EntityUid uid, EnsnaringComponent component, ThrowDoHitEvent args)
    {
        if (!component.CanThrowTrigger)
            return;

        TryEnsnare(args.Target, component);
    }

    /// <summary>
    /// Used where you want to try to ensnare an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="target">The entity that will be ensnared</param>
    /// <param name="component">The ensnaring component</param>
    public void TryEnsnare(EntityUid target, EnsnaringComponent component)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable))
            return;

        component.Ensnared = target;
        ensnareable.Container.Insert(component.Owner);
        ensnareable.IsEnsnared = true;
        Dirty(ensnareable);

        UpdateAlert(ensnareable);
        var ev = new EnsnareEvent(component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(target, ev);
    }

    /// <summary>
    /// Used where you want to try to free an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="target">The entity that will be free</param>
    /// <param name="component">The ensnaring component</param>
    public void TryFree(EntityUid target, EnsnaringComponent component, EntityUid? user = null)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!HasComp<EnsnareableComponent>(target))
            return;

        if (component.CancelToken != null)
            return;

        component.CancelToken = new CancellationTokenSource();

        var isOwner = !(user != null && target != user);
        var freeTime = isOwner ? component.BreakoutTime : component.FreeTime;
        bool breakOnMove;

        if (isOwner)
            breakOnMove = !component.CanMoveBreakout;
        else
            breakOnMove = true;

        var doAfterEventArgs = new DoAfterEventArgs(target, freeTime, component.CancelToken.Token, target)
        {
            BreakOnUserMove = breakOnMove,
            BreakOnTargetMove = breakOnMove,
            BreakOnDamage = false,
            BreakOnStun = true,
            NeedHand = true,
            TargetFinishedEvent = new FreeEnsnareDoAfterComplete(component.Owner),
            TargetCancelledEvent = new FreeEnsnareDoAfterCancel(component.Owner),
        };

        _doAfter.DoAfter(doAfterEventArgs);

        if (isOwner)
            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free", ("ensnare", component.Owner)), target, target);

        if (!isOwner && user != null)
        {
            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-other", ("ensnare", component.Owner), ("user", Identity.Entity(target, EntityManager))), user.Value, user.Value);
        }
    }

    /// <summary>
    /// Used to force free someone for things like if the <see cref="EnsnaringComponent"/> is removed
    /// </summary>
    public void ForceFree(EnsnaringComponent component)
    {
        if (!TryComp<EnsnareableComponent>(component.Ensnared, out var ensnareable))
            return;

        ensnareable.Container.ForceRemove(component.Owner);
        ensnareable.IsEnsnared = false;
        Dirty(ensnareable);
        component.Ensnared = null;

        UpdateAlert(ensnareable);
        var ev = new EnsnareRemoveEvent();
        RaiseLocalEvent(component.Owner, ev);
    }

    public void UpdateAlert(EnsnareableComponent component)
    {
        if (!component.IsEnsnared)
        {
            _alerts.ClearAlert(component.Owner, AlertType.Ensnared);
        }
        else
        {
            _alerts.ShowAlert(component.Owner, AlertType.Ensnared);
        }
    }
}
