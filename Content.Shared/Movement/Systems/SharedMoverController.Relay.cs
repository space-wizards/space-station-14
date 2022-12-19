using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentGetState>(OnRelayGetState);
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentHandleState>(OnRelayHandleState);
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentShutdown>(OnRelayShutdown);

        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentGetState>(OnTargetRelayGetState);
        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentHandleState>(OnTargetRelayHandleState);
        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentShutdown>(OnTargetRelayShutdown);
    }

    /// <summary>
    ///     Sets the relay entity and marks the component as dirty. This only exists because people have previously
    ///     forgotten to Dirty(), so fuck you, you have to use this method now.
    /// </summary>
    public void SetRelay(EntityUid uid, EntityUid relayEntity, RelayInputMoverComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.RelayEntity == relayEntity)
            return;

        if (uid == relayEntity)
        {
            Logger.Error($"An entity attempted to relay movement to itself. Entity:{ToPrettyString(uid)}");
            return;
        }

        if (TryComp<MovementRelayTargetComponent>(relayEntity, out var targetComp))
        {
            targetComp.Entities.Remove(uid);

            if (targetComp.Entities.Count == 0)
                RemComp<MovementRelayTargetComponent>(relayEntity);
        }

        component.RelayEntity = relayEntity;
        targetComp = EnsureComp<MovementRelayTargetComponent>(relayEntity);
        targetComp.Entities.Add(uid);
        Dirty(component);
        Dirty(targetComp);
    }

    private void OnRelayShutdown(EntityUid uid, RelayInputMoverComponent component, ComponentShutdown args)
    {
        // If relay is removed then cancel all inputs.
        if (!TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover))
            return;

        if (TryComp<MovementRelayTargetComponent>(component.RelayEntity, out var targetComp) &&
            targetComp.LifeStage < ComponentLifeStage.Stopping)
        {
            targetComp.Entities.Remove(uid);

            if (targetComp.Entities.Count == 0)
                RemCompDeferred<MovementRelayTargetComponent>(component.RelayEntity.Value);
            else
                Dirty(targetComp);
        }

        SetMoveInput(inputMover, MoveButtons.None);
    }

    private void OnRelayHandleState(EntityUid uid, RelayInputMoverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RelayInputMoverComponentState state) return;

        DebugTools.Assert(state.Entity != uid);
        component.RelayEntity = state.Entity;
    }

    private void OnRelayGetState(EntityUid uid, RelayInputMoverComponent component, ref ComponentGetState args)
    {
        args.State = new RelayInputMoverComponentState()
        {
            Entity = component.RelayEntity,
        };
    }

    #region Target Relay

    private void OnTargetRelayShutdown(EntityUid uid, MovementRelayTargetComponent component, ComponentShutdown args)
    {
        if (component.Entities.Count == 0)
            return;

        var relayQuery = GetEntityQuery<RelayInputMoverComponent>();

        foreach (var ent in component.Entities)
        {
            if (!relayQuery.TryGetComponent(ent, out var relay))
                continue;

            DebugTools.Assert(relay.RelayEntity == uid);

            if (relay.RelayEntity != uid)
                continue;

            RemCompDeferred<RelayInputMoverComponent>(ent);
        }
    }

    private void OnTargetRelayHandleState(EntityUid uid, MovementRelayTargetComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MovementRelayTargetComponentState state)
            return;

        component.Entities.Clear();
        component.Entities.AddRange(state.Entities);
    }

    private void OnTargetRelayGetState(EntityUid uid, MovementRelayTargetComponent component, ref ComponentGetState args)
    {
        args.State = new MovementRelayTargetComponentState(component.Entities);
    }

    #endregion

    [Serializable, NetSerializable]
    private sealed class RelayInputMoverComponentState : ComponentState
    {
        public EntityUid? Entity;
    }

    [Serializable, NetSerializable]
    private sealed class MovementRelayTargetComponentState : ComponentState
    {
        public List<EntityUid> Entities;

        public MovementRelayTargetComponentState(List<EntityUid> entities)
        {
            Entities = entities;
        }
    }
}
