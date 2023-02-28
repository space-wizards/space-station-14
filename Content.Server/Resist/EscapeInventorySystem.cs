using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Contests;
using Robust.Shared.Containers;
using Content.Server.Popups;
using Content.Shared.Storage;
using Content.Shared.Inventory;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Events;
using Content.Shared.Interaction.Events;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;

    /// <summary>
    /// You can't escape the hands of an entity this many times more massive than you.
    /// </summary>
    public const float MaximumMassDisadvantage = 6f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DoAfterEvent<EscapeInventoryEvent>>(OnEscape);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, ref MoveInputEvent args)
    {
        if (!_containerSystem.TryGetContainingContainer(uid, out var container) || !_actionBlockerSystem.CanInteract(uid, container.Owner))
            return;

        // Contested
        if (_handsSystem.IsHolding(container.Owner, uid, out var inHand))
        {
            var contestResults = _contests.MassContest(uid, container.Owner);

            // Inverse if we aren't going to divide by 0, otherwise just use a default multiplier of 1.
            if (contestResults != 0)
                contestResults = 1 / contestResults;
            else
                contestResults = 1;

            if (contestResults >= MaximumMassDisadvantage)
                return;

            AttemptEscape(uid, container.Owner, component, contestResults);
            return;
        }

        // Uncontested
        if (HasComp<SharedStorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner))
            AttemptEscape(uid, container.Owner, component);
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component, float multiplier = 1f)
    {
        if (component.IsEscaping)
            return;

        component.CancelToken = new CancellationTokenSource();
        component.IsEscaping = true;
        var escapeEvent = new EscapeInventoryEvent();
        var doAfterEventArgs = new DoAfterEventArgs(user, component.BaseResistTime * multiplier, cancelToken: component.CancelToken.Token, target:container)
        {
            BreakOnTargetMove = false,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = false
        };

        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), user, user);
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);
        _doAfterSystem.DoAfter(doAfterEventArgs, escapeEvent);
    }

    private void OnEscape(EntityUid uid, CanEscapeInventoryComponent component, DoAfterEvent<EscapeInventoryEvent> args)
    {
        if (args.Cancelled)
        {
            component.CancelToken = null;
            component.IsEscaping = false;
            return;
        }

        if (args.Handled)
            return;

        Transform(uid).AttachParentToContainerOrGrid(EntityManager);

        component.CancelToken = null;
        component.IsEscaping = false;
        args.Handled = true;
    }

    private void OnDropped(EntityUid uid, CanEscapeInventoryComponent component, DroppedEvent args)
    {
        component.CancelToken?.Cancel();
        component.CancelToken = null;
    }

    private sealed class EscapeInventoryEvent : EntityEventArgs
    {

    }
}
