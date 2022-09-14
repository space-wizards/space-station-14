using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentGetState>(OnRelayGetState);
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentHandleState>(OnRelayHandleState);
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentShutdown>(OnRelayShutdown);
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
