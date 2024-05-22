using Content.Client.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Client.Power.EntitySystems;

public abstract class PowerReceiverSystem : SharedPowerReceiverSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, ApcPowerReceiverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ApcPowerReceiverComponentState state)
            return;

        component.Powered = state.Powered;
    }
}
