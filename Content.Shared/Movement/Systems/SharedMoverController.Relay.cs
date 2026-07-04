using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentShutdown>(OnRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentShutdown>(OnTargetRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, AfterAutoHandleStateEvent>(OnAfterRelayTargetState);
        SubscribeLocalEvent<RelayInputMoverComponent, AfterAutoHandleStateEvent>(OnAfterRelayState);
        SubscribeLocalEvent<RelayInputMoverComponent, CanMoveUpdatedEvent>(OnRelayCanMoveUpdated);
        SubscribeLocalEvent<InputMoverComponent, CanMoveUpdatedEvent>(OnInputMoverCanMoveUpdated);
    }

    private void OnAfterRelayTargetState(Entity<MovementRelayTargetComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
    }

    private void OnAfterRelayState(Entity<RelayInputMoverComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
    }

    private void OnRelayCanMoveUpdated(Entity<RelayInputMoverComponent> ent, ref CanMoveUpdatedEvent args)
    {
        // Relay can-move state to the active mover target, not just the source
        RaiseLocalEvent(ent.Comp.RelayEntity, ref args);
    }

    protected virtual void OnInputMoverCanMoveUpdated(Entity<InputMoverComponent> ent, ref CanMoveUpdatedEvent args)
    {
        if (!args.CanMove)
        {
            // Remove from active mover query when entity cannot move
            RemCompDeferred<ActiveInputMoverComponent>(ent);
            return;
        }

        UpdateMoverStatus((ent, ent.Comp));
    }

    /// <summary>
    ///     Sets the relay entity and marks the component as dirty. This only exists because people have previously
    ///     forgotten to Dirty(), so fuck you, you have to use this method now.
    /// </summary>
    public void SetRelay(EntityUid uid, EntityUid relayEntity)
    {
        if (uid == relayEntity)
        {
            Log.Error($"An entity attempted to relay movement to itself. Entity:{ToPrettyString(uid)}");
            return;
        }

        var component = EnsureComp<RelayInputMoverComponent>(uid);
        var oldEffectiveMover = GetEffectiveMover((uid, component));
        if (component.RelayEntity == relayEntity)
            return;

        if (RelayTargetQuery.TryComp(component.RelayEntity, out var oldTarget))
        {
            oldTarget.Source = EntityUid.Invalid;
            RemComp(component.RelayEntity, oldTarget);
            PhysicsSystem.UpdateIsPredicted(component.RelayEntity);
        }

        var targetComp = EnsureComp<MovementRelayTargetComponent>(relayEntity);
        if (RelayQuery.TryComp(targetComp.Source, out var oldRelay))
        {
            var oldRelayEffectiveMover = GetEffectiveMover((targetComp.Source, oldRelay));
            oldRelay.RelayEntity = EntityUid.Invalid;
            RemComp(targetComp.Source, oldRelay);
            PhysicsSystem.UpdateIsPredicted(targetComp.Source);
            RaiseEffectiveMoverChanged(targetComp.Source, oldRelayEffectiveMover, targetComp.Source);
        }

        PhysicsSystem.UpdateIsPredicted(uid);
        PhysicsSystem.UpdateIsPredicted(relayEntity);
        component.RelayEntity = relayEntity;
        targetComp.Source = uid;
        Dirty(uid, component);
        Dirty(relayEntity, targetComp);
        _blocker.UpdateCanMove(uid);
        UpdateMoverStatus((relayEntity, null, targetComp));
        RaiseEffectiveMoverChanged(uid, oldEffectiveMover, relayEntity);
    }

    /// <summary>
    ///     Returns the entity whose movement should be treated as the effective movement source for <paramref name="mover"/>.
    ///     If the entity is relaying movement to another entity, returns that relay target, otherwise returns the entity itself.
    /// </summary>
    public EntityUid GetEffectiveMover(Entity<RelayInputMoverComponent?> mover)
    {
        if (RelayQuery.Resolve(mover.Owner, ref mover.Comp, false)
            && mover.Comp.RelayEntity.IsValid()
            && Exists(mover.Comp.RelayEntity))
        {
            return mover.Comp.RelayEntity;
        }

        return mover.Owner;
    }

    private void OnRelayShutdown(Entity<RelayInputMoverComponent> entity, ref ComponentShutdown args)
    {
        var oldEffectiveMover = entity.Comp.RelayEntity;
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        PhysicsSystem.UpdateIsPredicted(entity.Comp.RelayEntity);

        if (MoverQuery.TryComp(entity.Comp.RelayEntity, out var inputMover))
            SetMoveInput((entity.Comp.RelayEntity, inputMover), MoveButtons.None);

        if (Timing.ApplyingState)
            return;

        if (RelayTargetQuery.TryComp(entity.Comp.RelayEntity, out var target) && target.LifeStage <= ComponentLifeStage.Running)
            RemComp(entity.Comp.RelayEntity, target);

        _blocker.UpdateCanMove(entity.Owner);
        RaiseEffectiveMoverChanged(entity.Owner, oldEffectiveMover, entity.Owner);
    }

    protected virtual void OnTargetRelayShutdown(Entity<MovementRelayTargetComponent> entity, ref ComponentShutdown args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        PhysicsSystem.UpdateIsPredicted(entity.Comp.Source);

        if (Timing.ApplyingState)
            return;

        if (MoverQuery.TryComp(entity.Owner, out var inputMover))
            SetMoveInput((entity.Owner, inputMover), MoveButtons.None);

        if (RelayQuery.TryComp(entity.Comp.Source, out var relay) && relay.LifeStage <= ComponentLifeStage.Running)
        {
            RemComp(entity.Comp.Source, relay);
            RaiseEffectiveMoverChanged(entity.Comp.Source, entity.Owner, entity.Comp.Source);
        }
    }

    protected virtual void UpdateMoverStatus(Entity<InputMoverComponent?, MovementRelayTargetComponent?> ent) { }

    /// <summary>
    ///     Raises an event when the effective mover changes.
    ///     Used to cancel move-sensitive do-afters.
    /// </summary>
    private void RaiseEffectiveMoverChanged(EntityUid uid, EntityUid oldMover, EntityUid newMover)
    {
        if (oldMover == newMover)
            return;

        var ev = new EffectiveMoverChangedEvent(oldMover, newMover);
        RaiseLocalEvent(uid, ref ev);
    }
}
