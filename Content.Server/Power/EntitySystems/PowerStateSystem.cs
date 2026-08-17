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

        var isPowered = SetPowerLoadGetIsPowered(ent, ent.Comp.IsWorking);
        UpdateAppearance(ent, isPowered);
    }

    /// <summary> Updates appearance according to powered situation. </summary>
    [SubscribeLocalEvent]
    private void ReceivedChanged(Entity<PowerStateComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (!ent.Comp.IsWorking)
            return;

        bool isPowered = args.ReceivedPower >= args.DrawRate;
        UpdateAppearance(ent, isPowered);
    }

    /// <inheritdoc/>>
    protected override bool SetPowerLoadGetIsPowered(Entity<PowerStateComponent> ent, bool isWorking)
    {
        var isPowered = base.SetPowerLoadGetIsPowered(ent, isWorking);
        if (isPowered)
            return isPowered;

        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
        {
            powerConsumer.DrawRate = isWorking ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw;
            return powerConsumer.DrawRate <= powerConsumer.ReceivedPower;
        }

        return false;
    }
}
