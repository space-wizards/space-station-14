using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.Power.EntitySystems;

/// <inheritdoc/>>
public sealed partial class PowerStateSystem : SharedPowerStateSystem
{
    /// <summary> Init IsWorking and power values on startup. </summary>
    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<PowerStateComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.EnsureApc)
            EnsureComp<ApcPowerReceiverComponent>(ent);

        SetPowerLoad(ent, ent.Comp.IsWorking);
    }

    /// <inheritdoc/>>
    protected override void SetPowerLoad(Entity<PowerStateComponent> ent, bool isWorking)
    {
        base.SetPowerLoad(ent, isWorking);

        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            powerConsumer.DrawRate = isWorking ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw;
    }
}
