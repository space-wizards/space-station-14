using Content.Shared.EscalatedGrab.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Robust.Shared.Timing;

namespace Content.Shared.EscalatedGrab.Systems;

/// <summary>
/// Manages grab escalation. Re-clicking pull on a target escalates the grab
/// through <see cref="GrabStage"/> tiers instead of releasing.
/// </summary>
public abstract class SharedEscalatedGrabSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullableComponent, PullGrabEscalateAttemptEvent>(OnEscalateAttempt);
        SubscribeLocalEvent<PullableComponent, AttemptStopPullingEvent>(OnAttemptStopPulling);
        SubscribeLocalEvent<GrabStateComponent, PullStoppedMessage>(OnPullStopped);
    }

    private void OnEscalateAttempt(EntityUid uid, PullableComponent component, ref PullGrabEscalateAttemptEvent args)
    {
        TryEscalate(args.Puller, args.Pulled);
    }

    private void OnAttemptStopPulling(EntityUid uid, PullableComponent component, ref AttemptStopPullingEvent args)
    {
        if (args.Cancelled || args.User == null)
            return;

        // Release keybind clears escalation and lets the pull stop.
        if (args.Force)
        {
            ClearEscalation(args.User.Value);
            return;
        }
    }

    private void OnPullStopped(EntityUid uid, GrabStateComponent component, PullStoppedMessage args)
    {
        RemComp<GrabStateComponent>(uid);
    }

    /// <summary>
    /// Attempts to escalate the grab to the next stage.
    /// Returns true if the grab was escalated or is already at max stage.
    /// </summary>
    public bool TryEscalate(EntityUid puller, EntityUid target)
    {
        if (TryComp<GrabStateComponent>(puller, out var existing) && existing.Target == target)
            return true;

        if (!_timing.IsFirstTimePredicted)
            return true;

        var state = EnsureComp<GrabStateComponent>(puller);
        state.Target = target;
        state.Stage = GrabStage.Aggressive;
        Dirty(puller, state);
        _popup.PopupPredicted(Loc.GetString("escalated-grab-aggressive"), target, puller);
        return true;
    }

    /// <summary>
    /// Returns the current <see cref="GrabStage"/> for a puller on a target.
    /// Defaults to <see cref="GrabStage.Pull"/> if no escalation exists.
    /// </summary>
    public GrabStage GetStage(EntityUid puller, EntityUid target)
    {
        if (TryComp<GrabStateComponent>(puller, out var comp) && comp.Target == target)
            return comp.Stage;

        return GrabStage.Pull;
    }

    /// <summary>
    /// Checks whether the puller has at least the given grab stage on the target.
    /// </summary>
    public bool HasStage(EntityUid puller, EntityUid target, GrabStage minimumStage)
    {
        return GetStage(puller, target) >= minimumStage;
    }

    /// <summary>
    /// Removes grab escalation from a puller.
    /// </summary>
    public void ClearEscalation(EntityUid puller)
    {
        RemComp<GrabStateComponent>(puller);
    }
}
