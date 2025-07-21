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

namespace Content.Shared._Starlight.Actions.Stasis;

/// <summary>
/// System that handles the freezing behavior of entities in stasis.
/// This system prevents entities with StasisFrozenComponent from performing most actions,
/// while still allowing them to use the exit stasis action.
/// </summary>
public abstract class SharedStasisFrozenSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Block various actions and interactions
        SubscribeLocalEvent<StasisFrozenComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, PickupAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, ThrowAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StasisFrozenComponent, ComponentShutdown>(UpdateCanMove);
        SubscribeLocalEvent<StasisFrozenComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<StasisFrozenComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, AttackAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, ChangeDirectionAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, EmoteAttemptEvent>(OnEmoteAttempt);
        SubscribeLocalEvent<StasisFrozenComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    /// <summary>
    /// Handles use attempts, allowing only the exit stasis action to proceed.
    /// </summary>
    private void OnUseAttempt(EntityUid uid, StasisFrozenComponent component, UseAttemptEvent args)
    {
        // If we get here, the entity with StasisFrozenComponent is trying to use an action
        if (!TryComp<StasisComponent>(uid, out var stasisComponent))
        {
            args.Cancel();
            return;
        }

        // Check if this is the exit stasis action
        if (stasisComponent.ExitStasisActionEntity != null && args.Used == stasisComponent.ExitStasisActionEntity)
        {
            // Allow the exit stasis action to proceed
            return;
        }

        args.Cancel();
    }

    /// <summary>
    /// Handles speech attempts, blocking them if the entity is muted.
    /// </summary>
    private void OnSpeakAttempt(EntityUid uid, StasisFrozenComponent component, SpeakAttemptEvent args)
    {
        if (!component.Muted)
        {
            return;
        }

        args.Cancel();
    }

    /// <summary>
    /// Handles various cancellable attempts, blocking them all.
    /// </summary>
    private void OnCancellableAttempt(EntityUid uid, StasisFrozenComponent component,
        CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Handles pull attempts, preventing the entity from being pulled.
    /// </summary>
    private void OnPullAttempt(EntityUid uid, StasisFrozenComponent component, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    /// Handles component startup, stopping any active pulls and updating movement state.
    /// </summary>
    private void OnStartup(EntityUid uid, StasisFrozenComponent component, ComponentStartup args)
    {
        if (TryComp<PullableComponent>(uid, out var pullable))
        {
            _pulling.TryStopPull(uid, pullable);
        }

        UpdateCanMove(uid, component, args);
    }

    /// <summary>
    /// Handles movement update events, preventing movement while in stasis.
    /// </summary>
    private void OnUpdateCanMove(EntityUid uid, StasisFrozenComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    /// <summary>
    /// Updates the entity's movement state.
    /// </summary>
    private void UpdateCanMove(EntityUid uid, StasisFrozenComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    /// <summary>
    /// Handles emote attempts, blocking them if the entity is muted.
    /// </summary>
    private void OnEmoteAttempt(EntityUid uid, StasisFrozenComponent component, EmoteAttemptEvent args)
    {
        if (component.Muted)
        {
            args.Cancel();
        }
    }

    /// <summary>
    /// Handles general interaction attempts, blocking them all except for skills.
    /// </summary>
    private void OnInteractAttempt(Entity<StasisFrozenComponent> ent, ref InteractionAttemptEvent args)
    {
        // Check if this is a skill interaction
        if (args.Target == null)
        {
            return;
        }

        // Block all other interactions
        args.Cancelled = true;
    }
}
