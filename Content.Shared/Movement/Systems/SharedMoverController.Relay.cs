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
    }

    /// <summary>
    ///     Sets the relay entity and marks the component as dirty. This only exists because people have previously
    ///     forgotten to Dirty(), so fuck you, you have to use this method now.
    /// </summary>
    public void SetRelay(EntityUid uid, EntityUid relayEntity, RelayInputMoverComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (uid == relayEntity)
        {
            Logger.Error($"An entity attempted to relay movement to itself. Entity:{ToPrettyString(uid)}");
            return;
        }

        component.RelayEntity = relayEntity;
        Dirty(component);
    }

    private void OnRelayShutdown(EntityUid uid, RelayInputMoverComponent component, ComponentShutdown args)
    {
        // If relay is removed then cancel all inputs.
        if (!TryComp<InputMoverComponent>(component.RelayEntity, out var inputMover)) return;
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

    [Serializable, NetSerializable]
    private sealed class RelayInputMoverComponentState : ComponentState
    {
        public EntityUid? Entity;
    }
}
