using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Handles logic for the battery interface on SMES/substations.
/// </summary>
/// <remarks>
/// <para>
/// These devices have interfaces that allow user to toggle input and output,
/// and configure charge/discharge power limits.
/// </para>
/// <para>
/// This system is not responsible for any power logic on its own,
/// it merely reconfigures parameters on <see cref="PowerNetworkBatteryComponent"/> from the UI.
/// </para>
/// </remarks>
public sealed class BatteryInterfaceSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly SharedBatterySystem _battery = null!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        Subs.BuiEvents<BatteryInterfaceComponent>(
            BatteryUiKey.Key,
            subs =>
            {
                subs.Event<BatterySetInputBreakerMessage>(HandleSetInputBreaker);
                subs.Event<BatterySetOutputBreakerMessage>(HandleSetOutputBreaker);

                subs.Event<BatterySetChargeRateMessage>(HandleSetChargeRate);
                subs.Event<BatterySetDischargeRateMessage>(HandleSetDischargeRate);
            });
    }

    private void HandleSetInputBreaker(Entity<BatteryInterfaceComponent> ent, ref BatterySetInputBreakerMessage args)
    {
        var netBattery = Comp<PowerNetworkBatteryComponent>(ent);
        netBattery.CanCharge = args.On;

        _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Actor):actor} set input breaker to {args.On} on {ToPrettyString(ent):target}");
    }

    private void HandleSetOutputBreaker(Entity<BatteryInterfaceComponent> ent, ref BatterySetOutputBreakerMessage args)
    {
        var netBattery = Comp<PowerNetworkBatteryComponent>(ent);
        netBattery.CanDischarge = args.On;

        _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Actor):actor} set output breaker to {args.On} on {ToPrettyString(ent):target}");
    }

    private void HandleSetChargeRate(Entity<BatteryInterfaceComponent> ent, ref BatterySetChargeRateMessage args)
    {
        var netBattery = Comp<PowerNetworkBatteryComponent>(ent);
        netBattery.MaxChargeRate = Math.Clamp(args.Rate, ent.Comp.MinChargeRate, ent.Comp.MaxChargeRate);
    }

    private void HandleSetDischargeRate(Entity<BatteryInterfaceComponent> ent, ref BatterySetDischargeRateMessage args)
    {
        var netBattery = Comp<PowerNetworkBatteryComponent>(ent);
        netBattery.MaxSupply = Math.Clamp(args.Rate, ent.Comp.MinSupply, ent.Comp.MaxSupply);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BatteryInterfaceComponent, BatteryComponent, PowerNetworkBatteryComponent>();

        while (query.MoveNext(out var uid, out var batteryInterface, out var battery, out var netBattery))
        {
            UpdateUI(uid, batteryInterface, battery, netBattery);
        }
    }

    private void UpdateUI(
        EntityUid uid,
        BatteryInterfaceComponent batteryInterface,
        BatteryComponent battery,
        PowerNetworkBatteryComponent netBattery)
    {
        if (!_uiSystem.IsUiOpen(uid, BatteryUiKey.Key))
            return;

        var currentCharge = _battery.GetCharge((uid, battery));
        _uiSystem.SetUiState(
            uid,
            BatteryUiKey.Key,
            new BatteryBuiState
            {
                Capacity = battery.MaxCharge,
                Charge = currentCharge,
                CanCharge = netBattery.CanCharge,
                CanDischarge = netBattery.CanDischarge,
                CurrentReceiving = netBattery.CurrentReceiving,
                CurrentSupply = netBattery.CurrentSupply,
                MaxSupply = netBattery.MaxSupply,
                MaxChargeRate = netBattery.MaxChargeRate,
                Efficiency = netBattery.Efficiency,
                MaxMaxSupply = batteryInterface.MaxSupply,
                MinMaxSupply = batteryInterface.MinSupply,
                MaxMaxChargeRate = batteryInterface.MaxChargeRate,
                MinMaxChargeRate = batteryInterface.MinChargeRate,
                SupplyingNetworkHasPower = CheckHasPower<BatteryChargerComponent>(uid),
                LoadingNetworkHasPower = CheckHasPower<BatteryDischargerComponent>(uid),
            });

        return;

        bool CheckHasPower<TComp>(EntityUid entity) where TComp : BasePowerNetComponent
        {
            if (!TryComp(entity, out TComp? comp))
                return false;

            if (comp.Net == null)
                return false;

            return comp.Net.NetworkNode.LastCombinedMaxSupply > 0;
        }
    }
}
