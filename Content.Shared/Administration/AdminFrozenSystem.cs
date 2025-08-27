using Content.Shared.ActionBlocker;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Speech;
using Content.Shared.Throwing;

namespace Content.Shared.Administration;

// TODO deduplicate with BlockMovementComponent
public sealed class AdminFrozenSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AdminFrozenComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AdminFrozenComponent, ComponentShutdown>(UpdateCanMove);
        SubscribeLocalEvent<AdminFrozenComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<AdminFrozenComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, EmoteAttemptEvent>(OnEmoteAttempt);
        SubscribeLocalEvent<AdminFrozenComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    /// <summary>
    /// Freezes and mutes the given entity.
    /// </summary>
    public void FreezeAndMute(EntityUid uid)
    {
        var comp = EnsureComp<AdminFrozenComponent>(uid);
        comp.Muted = true;
        Dirty(uid, comp);
    }

    private void OnInteractAttempt(Entity<AdminFrozenComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnSpeakAttempt(EntityUid uid, AdminFrozenComponent component, SpeakAttemptEvent args)
    {
        if (!component.Muted)
            return;

        args.Cancel();
    }

    private void OnAttempt(EntityUid uid, AdminFrozenComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, AdminFrozenComponent component, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStartup(EntityUid uid, AdminFrozenComponent component, ComponentStartup args)
    {
        if (TryComp<PullableComponent>(uid, out var pullable))
        {
            _pulling.TryStopPull(uid, pullable);
        }

        UpdateCanMove(uid, component, args);
    }

    private void OnUpdateCanMove(EntityUid uid, AdminFrozenComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void UpdateCanMove(EntityUid uid, AdminFrozenComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    private void OnEmoteAttempt(EntityUid uid, AdminFrozenComponent component, EmoteAttemptEvent args)
    {
        if (component.Muted)
            args.Cancel();
    }
}
