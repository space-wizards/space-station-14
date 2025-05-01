using Content.Shared.Movement.Components;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentShutdown>(OnRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentShutdown>(OnTargetRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, AfterAutoHandleStateEvent>(OnAfterRelayTargetState);
        SubscribeLocalEvent<RelayInputMoverComponent, AfterAutoHandleStateEvent>(OnAfterRelayState);
    }

    private void OnAfterRelayTargetState(Entity<MovementRelayTargetComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
    }

    private void OnAfterRelayState(Entity<RelayInputMoverComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
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
    }

    private void OnRelayShutdown(Entity<RelayInputMoverComponent> entity, ref ComponentShutdown args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        PhysicsSystem.UpdateIsPredicted(entity.Comp.RelayEntity);

        if (TryComp<InputMoverComponent>(entity.Comp.RelayEntity, out var inputMover))
            SetMoveInput((entity.Comp.RelayEntity, inputMover), MoveButtons.None);

        if (Timing.ApplyingState)
            return;

        if (TryComp(entity.Comp.RelayEntity, out MovementRelayTargetComponent? target) && target.LifeStage <= ComponentLifeStage.Running)
            RemComp(entity.Comp.RelayEntity, target);
    }

    private void OnTargetRelayShutdown(Entity<MovementRelayTargetComponent> entity, ref ComponentShutdown args)
    {
        PhysicsSystem.UpdateIsPredicted(entity.Owner);
        PhysicsSystem.UpdateIsPredicted(entity.Comp.Source);

        if (Timing.ApplyingState)
            return;

        if (TryComp(entity.Comp.Source, out RelayInputMoverComponent? relay) && relay.LifeStage <= ComponentLifeStage.Running)
            RemComp(entity.Comp.Source, relay);
    }
}
