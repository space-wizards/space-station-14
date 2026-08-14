using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.Power.EntitySystems;

/// <inheritdoc/>>
public sealed partial class PowerStateSystem : SharedPowerStateSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerStateComponent, ComponentStartup>(OnComponentStartup);
    }

    /// <inheritdoc/>>
    public override void SetWorkingState(Entity<PowerStateComponent?> ent, bool working, bool shouldRaiseEvent = true)
    {
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp))
            return;

        base.SetWorkingState(ent, working, shouldRaiseEvent);

        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            powerConsumer.DrawRate = working ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw;
    }

    /// <summary> Init IsWorking and power values on startup. </summary>
    private void OnComponentStartup(Entity<PowerStateComponent> ent, ref ComponentStartup args)
    {
        if(ent.Comp.EnsureApc)
            EnsureComp<ApcPowerReceiverComponent>(ent);

        SetWorkingState(ent.Owner, ent.Comp.IsWorking, false);
    }
}
