using Content.Server.Power.Components;
using Content.Shared.Cargo;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rejuvenate;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Responsible for <see cref="BatteryComponent"/>.
/// Unpredicted equivalent of <see cref="PredictedBatterySystem"/>.
/// If you make changes to this make sure to keep the two consistent.
/// </summary>
[UsedImplicitly]
public sealed partial class BatterySystem : SharedBatterySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BatteryComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BatteryComponent, RejuvenateEvent>(OnBatteryRejuvenate);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, RejuvenateEvent>(OnNetBatteryRejuvenate);
        SubscribeLocalEvent<BatteryComponent, PriceCalculationEvent>(CalculateBatteryPrice);
        SubscribeLocalEvent<BatteryComponent, ChangeChargeEvent>(OnChangeCharge);
        SubscribeLocalEvent<BatteryComponent, GetChargeEvent>(OnGetCharge);

        SubscribeLocalEvent<NetworkBatteryPreSync>(PreSync);
        SubscribeLocalEvent<NetworkBatteryPostSync>(PostSync);
    }

    private void OnInit(Entity<BatteryComponent> ent, ref ComponentInit args)
    {
        DebugTools.Assert(!HasComp<PredictedBatteryComponent>(ent), $"{ent} has both BatteryComponent and PredictedBatteryComponent");
    }
    private void OnNetBatteryRejuvenate(Entity<PowerNetworkBatteryComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.NetworkBattery.CurrentStorage = ent.Comp.NetworkBattery.Capacity;
    }
    private void OnBatteryRejuvenate(Entity<BatteryComponent> ent, ref RejuvenateEvent args)
    {
        SetCharge(ent.AsNullable(), ent.Comp.MaxCharge);
    }

    private void OnExamine(Entity<BatteryComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!HasComp<ExaminableBatteryComponent>(ent))
            return;

        var chargePercentRounded = 0;
        if (ent.Comp.MaxCharge != 0)
            chargePercentRounded = (int)(100 * ent.Comp.CurrentCharge / ent.Comp.MaxCharge);

        args.PushMarkup(
            Loc.GetString(
                "examinable-battery-component-examine-detail",
                ("percent", chargePercentRounded),
                ("markupPercentColor", "green")
            )
        );
    }

    private void PreSync(NetworkBatteryPreSync ev)
    {
        // Ignoring entity pausing. If the entity was paused, neither component's data should have been changed.
        var enumerator = AllEntityQuery<PowerNetworkBatteryComponent, BatteryComponent>();
        while (enumerator.MoveNext(out var netBat, out var bat))
        {
            DebugTools.Assert(bat.CurrentCharge <= bat.MaxCharge && bat.CurrentCharge >= 0);
            netBat.NetworkBattery.Capacity = bat.MaxCharge;
            netBat.NetworkBattery.CurrentStorage = bat.CurrentCharge;
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

    /// <summary>
    /// Gets the price for the power contained in an entity's battery.
    /// </summary>
    private void CalculateBatteryPrice(Entity<BatteryComponent> ent, ref PriceCalculationEvent args)
    {
        args.Price += ent.Comp.CurrentCharge * ent.Comp.PricePerJoule;
    }

    private void OnChangeCharge(Entity<BatteryComponent> ent, ref ChangeChargeEvent args)
    {
        if (args.ResidualValue == 0)
            return;

        args.ResidualValue -= ChangeCharge(ent.AsNullable(), args.ResidualValue);
    }

    private void OnGetCharge(Entity<BatteryComponent> entity, ref GetChargeEvent args)
    {
        args.CurrentCharge += entity.Comp.CurrentCharge;
        args.MaxCharge += entity.Comp.MaxCharge;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BatterySelfRechargerComponent, BatteryComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp, out var bat))
        {
            if (!comp.AutoRecharge || IsFull((uid, bat)))
                continue;

            if (comp.NextAutoRecharge > curTime)
                continue;

            SetCharge((uid, bat), bat.CurrentCharge + comp.AutoRechargeRate * frameTime);
        }
    }
}
