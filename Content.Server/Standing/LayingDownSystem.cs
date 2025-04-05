using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Standing;

/// <remarks>Unfortunately cannot be shared because some standing conditions are server-side only</remarks>
public sealed class LayingDownSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly Shared.Standing.StandingStateSystem _standing = default!; // WHY IS THERE TWO DIFFERENT STANDING SYSTEMS?!
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding, InputCmdHandler.FromDelegate(ToggleStanding, handle: false, outsidePrediction: false))
            .Register<LayingDownSystem>();

        SubscribeLocalEvent<LayingDownComponent, StoodEvent>(DoRefreshMovementSpeed);
        SubscribeLocalEvent<LayingDownComponent, DownedEvent>(DoRefreshMovementSpeed);
        SubscribeLocalEvent<LayingDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<LayingDownComponent, EntParentChangedMessage>(OnParentChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<LayingDownSystem>();
    }


@DEATHB4DEFEATJul 9, 2024
@Anno-Midi
    private void DoRefreshMovementSpeed(EntityUid uid, LayingDownComponent component, object args)
    {
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, LayingDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (TryComp<StandingStateComponent>(uid, out var standingState) && standingState.Standing)
            return;

        args.ModifySpeed(component.DownedSpeedMultiplier, component.DownedSpeedMultiplier);
    }

    private void OnParentChanged(EntityUid uid, LayingDownComponent component, EntParentChangedMessage args)
    {
        // If the entity is not on a grid, try to make it stand up to avoid issues
        if (!TryComp<StandingStateComponent>(uid, out var standingState)
            || standingState.Standing
            || Transform(uid).GridUid != null)
            return;

        _standing.Stand(uid, standingState);
    }

    private void ToggleStanding(ICommonSession? session)
    {
        if (session is not { AttachedEntity: { Valid: true } uid } playerSession
            || !Exists(uid)
            || !TryComp<StandingStateComponent>(uid, out var standingState)
            || !TryComp<LayingDownComponent>(uid, out var layingDown))
            return;

        // If successful, show popup to self and others. Otherwise, only to self.
        if (ToggleStandingImpl(uid, standingState, layingDown, out var popupBranch))
        {
            _popups.PopupEntity(Loc.GetString($"laying-comp-{popupBranch}-other", ("entity", uid)), uid, Filter.PvsExcept(uid), true);
            layingDown.NextToggleAttempt = _timing.CurTime + layingDown.Cooldown;
        }

        _popups.PopupEntity(Loc.GetString($"laying-comp-{popupBranch}-self", ("entity", uid)), uid, uid);
    }

    private bool ToggleStandingImpl(EntityUid uid, StandingStateComponent standingState, LayingDownComponent layingDown, out string popupBranch)
    {
        var success = layingDown.NextToggleAttempt <= _timing.CurTime;

        if (_standing.IsDown(uid, standingState))
        {
            success = success && _standing.Stand(uid, standingState, force: false);
            popupBranch = success ? "stand-success" : "stand-fail";
        }
        else
        {
            success = success && Transform(uid).GridUid != null; // Do not allow laying down when not on a surface.
            success = success && _standing.Down(uid, standingState: standingState, playSound: true, dropHeldItems: false);
            popupBranch = success ? "lay-success" : "lay-fail";
        }

        return success;
    }
}

