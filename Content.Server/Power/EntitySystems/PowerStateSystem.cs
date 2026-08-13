using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.Power.EntitySystems;

public sealed class PowerStateSystem : SharedPowerStateSystem
{
    public override void SetWorkingState(Entity<PowerStateComponent?> ent, bool working)
    {
        base.SetWorkingState(ent, working);

        if (!_powerStateQuery.Resolve(ent, ref ent.Comp))
            return;

        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            powerConsumer.DrawRate = ent.Comp!.WorkingPowerDraw;
    }

    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<PowerStateComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<ApcPowerReceiverComponent>(ent);
        SetWorkingState(ent.Owner, ent.Comp.IsWorking);
    }
}
