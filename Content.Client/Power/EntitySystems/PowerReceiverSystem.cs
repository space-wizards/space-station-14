using Content.Client.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Examine;
using Robust.Shared.GameStates;

namespace Content.Client.Power.EntitySystems;

public sealed class PowerReceiverSystem : SharedPowerReceiverSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ApcPowerReceiverComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ApcPowerReceiverComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnExamined(Entity<ApcPowerReceiverComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(GetExamineText(ent.Comp.Powered));
    }

    private void OnHandleState(EntityUid uid, ApcPowerReceiverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ApcPowerReceiverComponentState state)
            return;

        component.Powered = state.Powered;
    }
}
