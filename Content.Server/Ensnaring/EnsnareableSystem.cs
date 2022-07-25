using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Ensnaring.Components;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Ensnaring;

public sealed class EnsnareableSystem : SharedEnsnareableSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableComponent, ComponentInit>(OnEnsnareableInit);
        SubscribeLocalEvent<EnsnareableComponent, FreeEnsnareDoAfterComplete>(OnFreeComplete);
        SubscribeLocalEvent<EnsnareableComponent, FreeEnsnareDoAfterCancel>(OnFreeFail);
    }

    private void OnEnsnareableInit(EntityUid uid, EnsnareableComponent component, ComponentInit args)
    {
        component.Container = _container.EnsureContainer<Container>(component.Owner, "Ensnare Container");
    }

    /// <summary>
    /// Used where you want to try to free an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="ensnaringEntity">The entity that was used to ensnare</param>
    /// <param name="target">The entity that will be free</param>
    /// <param name="component">The ensnaring component</param>
    public void TryFree(EntityUid ensnaringEntity, EntityUid target, EnsnaringComponent component)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable))
            return;

        if (component.CancelToken != null)
            return;

        component.CancelToken = new CancellationTokenSource();

        var doAfterEventArgs = new DoAfterEventArgs(target, component.BreakoutTime, component.CancelToken.Token, target)
        {
            BreakOnUserMove = !component.CanMoveBreakout,
            BreakOnTargetMove = !component.CanMoveBreakout,
            BreakOnDamage = false,
            BreakOnStun = true,
            NeedHand = true,
            TargetFinishedEvent = new FreeEnsnareDoAfterComplete(component),
            TargetCancelledEvent = new FreeEnsnareDoAfterCancel(component),
        };

        _doAfter.DoAfter(doAfterEventArgs);

        _popup.PopupEntity(Loc.GetString("ensnare-component-try-free", ("ensnare", component.Owner)), target, Filter.Entities(target));
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

    private void OnFreeComplete(EntityUid uid, EnsnareableComponent component, FreeEnsnareDoAfterComplete args)
    {
        if (args.EnsnaringComponent != null)
        {
            args.EnsnaringComponent.Ensnared = null;
            component.EnsnaringEntity = null;
            component.Container.ForceRemove(args.EnsnaringComponent.Owner);
            component.IsEnsnared = false;

            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-complete", ("ensnare", args.EnsnaringComponent.Owner)),
                uid, Filter.Entities(uid), PopupType.Large);

            UpdateAlert(component);
            var ev = new EnsnareChangeEvent(args.EnsnaringComponent.Owner, component.WalkSpeed, component.SprintSpeed);
            RaiseLocalEvent(uid, ev, false);

            args.EnsnaringComponent.CancelToken = null;
        }
    }

    private void OnFreeFail(EntityUid uid, EnsnareableComponent component, FreeEnsnareDoAfterCancel args)
    {
        if (args.EnsnaringComponent != null)
        {
            args.EnsnaringComponent.CancelToken = null;

            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-fail", ("ensnare", args.EnsnaringComponent.Owner)),
                uid, Filter.Entities(uid), PopupType.Large);
        }

    }

    private sealed class FreeEnsnareDoAfterComplete : EntityEventArgs
    {
        public readonly EnsnaringComponent? EnsnaringComponent;

        public FreeEnsnareDoAfterComplete(EnsnaringComponent ensnaringComponent)
        {
            EnsnaringComponent = ensnaringComponent;
        }
    }

    private sealed class FreeEnsnareDoAfterCancel : EntityEventArgs
    {
        public readonly EnsnaringComponent? EnsnaringComponent;

        public FreeEnsnareDoAfterCancel(EnsnaringComponent ensnaringComponent)
        {
            EnsnaringComponent = ensnaringComponent;
        }
    }
}
