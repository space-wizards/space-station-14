using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Input;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Standing;

public abstract class SharedLayingDownSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding, InputCmdHandler.FromDelegate(ToggleStanding))
            .Register<SharedLayingDownSystem>();

        SubscribeNetworkEvent<ChangeLayingDownEvent>(OnChangeState);

        SubscribeLocalEvent<StandingStateComponent, StandingUpDoAfterEvent>(OnStandingUpDoAfter);
        SubscribeLocalEvent<LayingDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<LayingDownComponent, EntParentChangedMessage>(OnParentChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SharedLayingDownSystem>();
    }

    private void ToggleStanding(ICommonSession? session)
    {
        if (session?.AttachedEntity == null ||
            !HasComp<LayingDownComponent>(session.AttachedEntity) ||
            _gravity.IsWeightless(session.AttachedEntity.Value))
        {
            return;
        }

        RaiseNetworkEvent(new ChangeLayingDownEvent());
    }

    private void OnChangeState(ChangeLayingDownEvent ev, EntitySessionEventArgs args)
    {
        if (!args.SenderSession.AttachedEntity.HasValue)
            return;

        var uid = args.SenderSession.AttachedEntity.Value;

        // TODO: Wizard
        //if (HasComp<FrozenComponent>(uid))
        //   return;

        if (!TryComp(uid, out StandingStateComponent? standing) ||
            !TryComp(uid, out LayingDownComponent? layingDown))
        {
            return;
        }

        RaiseNetworkEvent(new CheckAutoGetUpEvent(GetNetEntity(uid)));

        if (HasComp<KnockedDownComponent>(uid) || !_mobState.IsAlive(uid))
            return;

        if (_standing.IsDown(uid, standing))
            TryStandUp(uid, layingDown, standing);
        else
            TryLieDown(uid, layingDown, standing);
    }

    private void OnStandingUpDoAfter(EntityUid uid, StandingStateComponent component, StandingUpDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || HasComp<KnockedDownComponent>(uid) ||
            _mobState.IsIncapacitated(uid) || !_standing.Stand(uid))
        {
            component.CurrentState = StandingState.Lying;
            return;
        }

        component.CurrentState = StandingState.Standing;
    }

    private void OnRefreshMovementSpeed(EntityUid uid, LayingDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_standing.IsDown(uid))
            args.ModifySpeed(component.SpeedModify, component.SpeedModify);
        else
            args.ModifySpeed(1f, 1f);
    }

    private void OnParentChanged(EntityUid uid, LayingDownComponent component, EntParentChangedMessage args)
    {
        // If the entity is not on a grid, try to make it stand up to avoid issues
        if (!TryComp<StandingStateComponent>(uid, out var standingState)
            || standingState.CurrentState is StandingState.Standing
            || Transform(uid).GridUid != null)
        {
            return;
        }

        _standing.Stand(uid, standingState);
    }

    public bool TryStandUp(EntityUid uid, LayingDownComponent? layingDown = null, StandingStateComponent? standingState = null)
    {
        if (!Resolve(uid, ref standingState, false) ||
            !Resolve(uid, ref layingDown, false) ||
            standingState.CurrentState is not StandingState.Lying ||
            !_mobState.IsAlive(uid) ||
            TerminatingOrDeleted(uid))
        {
            return false;
        }

        var args = new DoAfterArgs(EntityManager, uid, layingDown.StandingUpTime, new StandingUpDoAfterEvent(), uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = false,
            RequireCanInteract = false
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        standingState.CurrentState = StandingState.GettingUp;
        return true;
    }

    public bool TryLieDown(EntityUid uid, LayingDownComponent? layingDown = null, StandingStateComponent? standingState = null, DropHeldItemsBehavior behavior = DropHeldItemsBehavior.NoDrop)
    {
        if (!Resolve(uid, ref standingState, false) ||
            !Resolve(uid, ref layingDown, false) ||
            standingState.CurrentState is not StandingState.Standing)
        {
            if (behavior == DropHeldItemsBehavior.AlwaysDrop)
                RaiseLocalEvent(uid, new DropHandItemsEvent());

            return false;
        }

        _standing.Down(uid, true, behavior != DropHeldItemsBehavior.NoDrop, false, standingState);
        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial class StandingUpDoAfterEvent : SimpleDoAfterEvent;

public enum DropHeldItemsBehavior : byte
{
    NoDrop,
    DropIfStanding,
    AlwaysDrop
}
