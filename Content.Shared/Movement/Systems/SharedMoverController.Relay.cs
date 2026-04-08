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
        if (args.CanMove)
            return;

        if (MoverQuery.TryComp(ent.Comp.RelayEntity, out var inputMoverComponent))
            SetMoveInput((ent.Comp.RelayEntity, inputMoverComponent), MoveButtons.None);
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
        if (component.RelayEntity == relayEntity)
            return;

        if (TryComp(component.RelayEntity, out MovementRelayTargetComponent? oldTarget))
        {
            oldTarget.Source = EntityUid.Invalid;
            RemComp(component.RelayEntity, oldTarget);
            PhysicsSystem.UpdateIsPredicted(component.RelayEntity);
        }

        var targetComp = EnsureComp<MovementRelayTargetComponent>(relayEntity);
        if (TryComp(targetComp.Source, out RelayInputMoverComponent? oldRelay))
        {
            oldRelay.RelayEntity = EntityUid.Invalid;
            RemComp(targetComp.Source, oldRelay);
            PhysicsSystem.UpdateIsPredicted(targetComp.Source);
        }

        PhysicsSystem.UpdateIsPredicted(uid);
        PhysicsSystem.UpdateIsPredicted(relayEntity);
        component.RelayEntity = relayEntity;
        targetComp.Source = uid;
        Dirty(uid, component);
        Dirty(relayEntity, targetComp);
        _blocker.UpdateCanMove(uid);
        UpdateMoverStatus((relayEntity, null, targetComp));
    }

    private void OnRelayShutdown(Entity<RelayInputMoverComponent> entity, ref ComponentShutdown args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        PhysicsSystem.UpdateIsPredicted(entity.Comp.RelayEntity);

        if (MoverQuery.TryComp(entity.Comp.RelayEntity, out var inputMover))
            SetMoveInput((entity.Comp.RelayEntity, inputMover), MoveButtons.None);

        if (Timing.ApplyingState)
            return;

        if (RelayTargetQuery.TryComp(entity.Comp.RelayEntity, out var target) && target.LifeStage <= ComponentLifeStage.Running)
            RemComp(entity.Comp.RelayEntity, target);

        _blocker.UpdateCanMove(entity.Owner);
    }

    protected virtual void OnTargetRelayShutdown(Entity<MovementRelayTargetComponent> entity, ref ComponentShutdown args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        PhysicsSystem.UpdateIsPredicted(entity.Comp.Source);

        if (MoverQuery.TryComp(entity.Owner, out var inputMover))
            SetMoveInput((entity.Owner, inputMover), MoveButtons.None);

        if (Timing.ApplyingState)
            return;

        if (RelayQuery.TryComp(entity.Comp.Source, out var relay) && relay.LifeStage <= ComponentLifeStage.Running)
            RemComp(entity.Comp.Source, relay);
    }

    protected virtual void UpdateMoverStatus(Entity<InputMoverComponent?, MovementRelayTargetComponent?> ent) { }
}
