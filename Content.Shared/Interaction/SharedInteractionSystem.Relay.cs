using Content.Shared.Interaction.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Interaction;

public abstract partial class SharedInteractionSystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<InteractionRelayComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<InteractionRelayComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, InteractionRelayComponent component, ref ComponentGetState args)
    {
        args.State = new InteractionRelayComponentState(component.RelayEntity);
    }

    private void OnHandleState(EntityUid uid, InteractionRelayComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not InteractionRelayComponentState state)
            return;

        component.RelayEntity = state.RelayEntity;
    }

    public void SetRelay(EntityUid uid, EntityUid? relayEntity, InteractionRelayComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.RelayEntity = relayEntity;
        Dirty(component);
    }
}
