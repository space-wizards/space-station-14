using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

public sealed class BatterySystem : SharedBatterySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerNetworkBatteryComponent, RejuvenateEvent>(OnNetBatteryRejuvenate);
        SubscribeLocalEvent<NetworkBatteryPreSync>(PreSync);
        SubscribeLocalEvent<NetworkBatteryPostSync>(PostSync);
    }

    protected override void OnStartup(Entity<BatteryComponent> ent, ref ComponentStartup args)
    {
        // Debug assert to prevent anyone from killing their networking performance by dirtying a battery's charge every single tick.
        // This checks for components that interact with the power network, have a charge rate that ramps up over time and therefore
        // have to set the charge in an update loop instead of using a <see cref="RefreshChargeRateEvent"/> subscription.
        // This is usually the case for APCs, SMES, battery powered turrets or similar.
        // For those entities you should disable net sync for the battery in your prototype, using
        /// <code>
        /// - type: Battery
        ///   netSync: false
        /// </code>
        /// This disables networking and prediction for this battery.
        if (!ent.Comp.NetSyncEnabled)
            return;

        DebugTools.Assert(!HasComp<ApcPowerReceiverBatteryComponent>(ent), $"{ToPrettyString(ent.Owner)} has a predicted battery connected to the power net. Disable net sync!");
        DebugTools.Assert(!HasComp<PowerNetworkBatteryComponent>(ent), $"{ToPrettyString(ent.Owner)} has a predicted battery connected to the power net. Disable net sync!");
        DebugTools.Assert(!HasComp<PowerConsumerComponent>(ent), $"{ToPrettyString(ent.Owner)} has a predicted battery connected to the power net. Disable net sync!");
    }


    private void OnNetBatteryRejuvenate(Entity<PowerNetworkBatteryComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.NetworkBattery.CurrentStorage = ent.Comp.NetworkBattery.Capacity;
    }

    private void PreSync(NetworkBatteryPreSync ev)
    {
        // Ignoring entity pausing. If the entity was paused, neither component's data should have been changed.
        var enumerator = AllEntityQuery<PowerNetworkBatteryComponent, BatteryComponent>();
        while (enumerator.MoveNext(out var uid, out var netBat, out var bat))
        {
            var currentCharge = GetCharge((uid, bat));
            DebugTools.Assert(currentCharge <= bat.MaxCharge && currentCharge >= 0);
            netBat.NetworkBattery.Capacity = bat.MaxCharge;
            netBat.NetworkBattery.CurrentStorage = currentCharge;
        }
    }

    private void PostSync(NetworkBatteryPostSync ev)
    {
        // Ignoring entity pausing. If the entity was paused, neither component's data should have been changed.
        var enumerator = AllEntityQuery<PowerNetworkBatteryComponent, BatteryComponent>();
        while (enumerator.MoveNext(out var uid, out var netBat, out var bat))
        {
            SetCharge((uid, bat), netBat.NetworkBattery.CurrentStorage);
        }
    }
}
