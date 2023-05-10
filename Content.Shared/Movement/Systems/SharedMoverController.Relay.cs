using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private void InitializeRelay()
    {
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentShutdown>(OnRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentShutdown>(OnTargetRelayShutdown);
        SubscribeLocalEvent<MovementRelayTargetComponent, AfterAutoHandleStateEvent>(OnAfterRelayTargetState);
        SubscribeLocalEvent<RelayInputMoverComponent, AfterAutoHandleStateEvent>(OnAfterRelayState);
    }

    private void OnAfterRelayTargetState(EntityUid uid, MovementRelayTargetComponent component, ref AfterAutoHandleStateEvent args)
    {
        _physics.UpateIsPredicted(uid);
    }

    private void OnAfterRelayState(EntityUid uid, RelayInputMoverComponent component, ref AfterAutoHandleStateEvent args)
    {
        _physics.UpateIsPredicted(uid);
    }

    /// <summary>
    ///     Sets the relay entity and marks the component as dirty. This only exists because people have previously
    ///     forgotten to Dirty(), so fuck you, you have to use this method now.
    /// </summary>
    public void SetRelay(EntityUid uid, EntityUid relayEntity)
    {
        if (uid == relayEntity)
        {
            Logger.Error($"An entity attempted to relay movement to itself. Entity:{ToPrettyString(uid)}");
            return;
        }

        var component = EnsureComp<RelayInputMoverComponent>(uid);
        if (component.RelayEntity == relayEntity)
            return;

        if (TryComp(component.RelayEntity, out MovementRelayTargetComponent? oldTarget))
        {
            oldTarget.Source = EntityUid.Invalid;
            RemComp(component.RelayEntity, oldTarget);
            _physics.UpateIsPredicted(component.RelayEntity);
        }

        var targetComp = EnsureComp<MovementRelayTargetComponent>(relayEntity);
        if (TryComp(targetComp.Source, out RelayInputMoverComponent? oldRelay))
        {
            oldRelay.RelayEntity = EntityUid.Invalid;
            RemComp(targetComp.Source, oldRelay);
            _physics.UpateIsPredicted(targetComp.Source);
        }

        _physics.UpateIsPredicted(uid);
        _physics.UpateIsPredicted(relayEntity);
        component.RelayEntity = relayEntity;
        targetComp.Source = uid;
        Dirty(component);
        Dirty(targetComp);
    }

    private void OnRelayShutdown(EntityUid uid, RelayInputMoverComponent component, ComponentShutdown args)
    {
        _physics.UpateIsPredicted(uid);
        _physics.UpateIsPredicted(component.RelayEntity);

        if (TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover))
            SetMoveInput(inputMover, MoveButtons.None);

        if (_timing.ApplyingState)
            return;

        if (TryComp(component.RelayEntity, out MovementRelayTargetComponent? target) && target.LifeStage <= ComponentLifeStage.Running)
            RemComp(component.RelayEntity, target);
    }

    private void OnTargetRelayShutdown(EntityUid uid, MovementRelayTargetComponent component, ComponentShutdown args)
    {
        _physics.UpateIsPredicted(uid);
        _physics.UpateIsPredicted(component.Source);

        if (_timing.ApplyingState)
            return;

        if (TryComp(component.Source, out RelayInputMoverComponent? relay) && relay.LifeStage <= ComponentLifeStage.Running)
            RemComp(component.Source, relay);
    }
}
