using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.ActionBlocker;

namespace Content.Shared._Starlight.IdClothingBlocker;

/// <summary>
/// System handling being blocked by equipment and lack of permissions to use it.
/// </summary>
public abstract class SharedIdClothingFrozenSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdClothingFrozenComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<IdClothingFrozenComponent, AttackAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<IdClothingFrozenComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<IdClothingFrozenComponent, ComponentShutdown>(UpdateCanMove);
        SubscribeLocalEvent<IdClothingFrozenComponent, ChangeDirectionAttemptEvent>(OnCancellableAttempt);
        SubscribeLocalEvent<IdClothingFrozenComponent, UseAttemptEvent>(OnUseAttempt);
    }

    /// <summary>
    /// Handles component startup, stopping any active pulls and updating movement state.
    /// </summary>
    private void OnStartup(EntityUid uid, IdClothingFrozenComponent component, ComponentStartup args)
    {
        UpdateCanMove(uid, component, args);
    }

    /// <summary>
    /// Handles movement update events, preventing movement while frozen.
    /// </summary>
    private void OnUpdateCanMove(EntityUid uid, IdClothingFrozenComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    /// <summary>
    /// Updates the entity's movement state.
    /// </summary>
    private void UpdateCanMove(EntityUid uid, IdClothingFrozenComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    /// <summary>
    /// Handles various cancellable attempts, blocking them all.
    /// </summary>
    private void OnCancellableAttempt(EntityUid uid, IdClothingFrozenComponent component,
        CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Handles use attempts, blocking all usage while frozen.
    /// </summary>
    private void OnUseAttempt(EntityUid uid, IdClothingFrozenComponent component, UseAttemptEvent args)
    {
        args.Cancel();
    }
}