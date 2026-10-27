using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.Power.EntitySystems;

/// <inheritdoc/>
public sealed partial class ServerPowerStateSystem : PowerStateSystem
{
    [Dependency] private EntityQuery<PowerConsumerComponent> _powerConsumerQuery;

    /// <summary>
    /// Init IsWorking and power values on startup.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<PowerStateComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.EnsureApc)
            EnsureComp<ApcPowerReceiverComponent>(ent);

        var isPowered = SetPowerLoadGetHasPower(ent, ent.Comp.IsTurnedOn);
        UpdateAppearance(ent, isPowered);
    }

    /// <summary>
    /// Updates appearance according to powered situation.
    /// </summary>
    [SubscribeLocalEvent]
    private void ReceivedChanged(Entity<PowerStateComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (!ent.Comp.IsTurnedOn)
            return;

        var hasPower = args.ReceivedPower >= args.DrawRate;
        SetIsPoweredState(ent, hasPower);
    }

    /// <inheritdoc/>>
    protected override bool SetPowerLoadGetHasPower(Entity<PowerStateComponent> ent, bool isWorking)
    {
        var isPowered = base.SetPowerLoadGetHasPower(ent, isWorking);
        if (isPowered)
            return isPowered;

        if (!_powerConsumerQuery.TryComp(ent, out var powerConsumer))
            return false;

        powerConsumer.DrawRate = isWorking ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw;
        return powerConsumer.DrawRate <= powerConsumer.ReceivedPower;
    }
}
