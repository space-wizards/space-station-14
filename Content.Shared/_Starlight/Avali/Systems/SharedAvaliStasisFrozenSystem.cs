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
using Content.Shared.Starlight.Avali.Components;

namespace Content.Shared.Starlight.Avali.Systems;

/// <summary>
/// System that handles the freezing behavior of entities in stasis.
/// This system prevents entities with AvaliStasisFrozenComponent from performing most actions,
/// while still allowing them to use the exit stasis action.
/// </summary>
public abstract class SharedAvaliStasisFrozenSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("avali.stasis");

        // Block various actions and interactions
        SubscribeLocalEvent<AvaliStasisFrozenComponent, UseAttemptEvent>(OnUseAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, PickupAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, ThrowAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, ComponentShutdown>(UpdateCanMove);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, AttackAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, ChangeDirectionAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, EmoteAttemptEvent>(OnEmoteAttempt);
        SubscribeLocalEvent<AvaliStasisFrozenComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    /// <summary>
    /// Handles use attempts, allowing only the exit stasis action to proceed.
    /// </summary>
    private void OnUseAttempt(EntityUid uid, AvaliStasisFrozenComponent component, UseAttemptEvent args)
    {
        // If we get here, the entity with AvaliStasisFrozenComponent is trying to use an action
        if (!TryComp<AvaliStasisComponent>(uid, out var stasisComponent))
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
    private void OnSpeakAttempt(EntityUid uid, AvaliStasisFrozenComponent component, SpeakAttemptEvent args)
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
    private void OnCancellableAttempt(EntityUid uid, AvaliStasisFrozenComponent component,
        CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Handles pull attempts, preventing the entity from being pulled.
    /// </summary>
    private void OnPullAttempt(EntityUid uid, AvaliStasisFrozenComponent component, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    /// <summary>
    /// Handles component startup, stopping any active pulls and updating movement state.
    /// </summary>
    private void OnStartup(EntityUid uid, AvaliStasisFrozenComponent component, ComponentStartup args)
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
    private void OnUpdateCanMove(EntityUid uid, AvaliStasisFrozenComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    /// <summary>
    /// Updates the entity's movement state.
    /// </summary>
    private void UpdateCanMove(EntityUid uid, AvaliStasisFrozenComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    /// <summary>
    /// Handles emote attempts, blocking them if the entity is muted.
    /// </summary>
    private void OnEmoteAttempt(EntityUid uid, AvaliStasisFrozenComponent component, EmoteAttemptEvent args)
    {
        if (component.Muted)
        {
            args.Cancel();
        }
    }

    /// <summary>
    /// Handles general interaction attempts, blocking them all except for skills.
    /// </summary>
    private void OnInteractAttempt(Entity<AvaliStasisFrozenComponent> ent, ref InteractionAttemptEvent args)
    {
        _sawmill.Info($"Interaction attempt - args.Uid: {args.Uid}, ent.Owner: {ent.Owner}, args.Target: {args.Target}");

        // Check if this is a skill interaction
        if (args.Target == null)
        {
            _sawmill.Info("Allowing interaction - skill (no target)");
            return;
        }

        // Block all other interactions
        _sawmill.Info("Blocking interaction - not a skill");
        args.Cancelled = true;
    }
}