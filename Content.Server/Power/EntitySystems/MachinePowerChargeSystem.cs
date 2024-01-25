using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Power.EntitySystems;

public sealed class MachinePowerChargeSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MachinePowerChargeComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentShutdown(EntityUid uid, MachinePowerChargeComponent component, ComponentShutdown args)
    {
        if (!component.Active)
            return;

            component.Active = false;
            // TODO: Send deactivated event
    }

    private void OnCompInit(Entity<MachinePowerChargeComponent> ent, ref ComponentInit args)
    {
        ApcPowerReceiverComponent? powerReceiver = null;
        if (!Resolve(ent, ref powerReceiver, false))
            return;

        UpdatePowerState(ent, powerReceiver);
        UpdateState((ent, ent.Comp, powerReceiver));
    }

    private void SetSwitchedOn(EntityUid uid, MachinePowerChargeComponent component, bool on,
        ApcPowerReceiverComponent? powerReceiver = null, ICommonSession? session = null)
    {
        if (!Resolve(uid, ref powerReceiver))
            return;

        if (session is { AttachedEntity: { } })
            _adminLogger.Add(LogType.Action, on ? LogImpact.Medium : LogImpact.High, $"{session:player} set ${ToPrettyString(uid):target} to {(on ? "on" : "off")}");

        component.SwitchedOn = on;
        UpdatePowerState(component, powerReceiver);
        component.NeedUIUpdate = true;
    }

    private static void UpdatePowerState(MachinePowerChargeComponent component, ApcPowerReceiverComponent powerReceiver)
    {
        powerReceiver.Load = component.SwitchedOn ? component.ActivePowerUse : component.IdlePowerUse;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MachinePowerChargeComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var chargingMachine, out var powerReceiver))
        {
            var ent = (uid, gravGen: chargingMachine, powerReceiver);
            if (!chargingMachine.Intact)
                continue;

            // Calculate charge rate based on power state and such.
            // Negative charge rate means discharging.
            float chargeRate;
            if (chargingMachine.SwitchedOn)
            {
                if (powerReceiver.Powered)
                {
                    chargeRate = chargingMachine.ChargeRate;
                }
                else
                {
                    // Scale discharge rate such that if we're at 25% active power we discharge at 75% rate.
                    var receiving = powerReceiver.PowerReceived;
                    var mainSystemPower = Math.Max(0, receiving - chargingMachine.IdlePowerUse);
                    var ratio = 1 - mainSystemPower / (chargingMachine.ActivePowerUse - chargingMachine.IdlePowerUse);
                    chargeRate = -(ratio * chargingMachine.ChargeRate);
                }
            }
            else
            {
                chargeRate = -chargingMachine.ChargeRate;
            }

            var lastCharge = chargingMachine.Charge;
            chargingMachine.Charge =
                Math.Clamp(chargingMachine.Charge + frameTime * chargeRate, 0, chargingMachine.MaxCharge);
            if (chargeRate > 0)
            {
                // Charging.
                if (MathHelper.CloseTo(chargingMachine.Charge, chargingMachine.MaxCharge) && !chargingMachine.Active)
                {
                    chargingMachine.Active = true;
                }
            }
            else
            {
                // Discharging
                if (MathHelper.CloseTo(chargingMachine.Charge, 0) && chargingMachine.Active)
                {
                    chargingMachine.Active = false;
                }
            }

            var updateUI = chargingMachine.NeedUIUpdate;
            if (!MathHelper.CloseTo(lastCharge, chargingMachine.Charge))
            {
                UpdateState(ent);
                updateUI = true;
            }

            if (updateUI)
                UpdateUI(ent, chargeRate);
        }
    }

    private void UpdateState(Entity<MachinePowerChargeComponent, ApcPowerReceiverComponent> ent)
    {
        var (uid, grav, powerReceiver) = ent;
        var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);
        _appearance.SetData(uid, MachinePowerChargeVisuals.Charge, grav.Charge, appearance);


        if (!grav.Intact)
        {
            MakeBroken((uid, grav), appearance);
        }
        else if (powerReceiver.PowerReceived < grav.IdlePowerUse)
        {
            MakeUnpowered((uid, grav), appearance);
        }
        else if (!grav.SwitchedOn)
        {
            MakeOff((uid, grav), appearance);
        }
        else
        {
            MakeOn((uid, grav), appearance);
        }
    }

    private void UpdateUI(Entity<MachinePowerChargeComponent, ApcPowerReceiverComponent> ent, float chargeRate)
    {
        var (_, component, powerReceiver) = ent;
        if (!_uiSystem.IsUiOpen(ent, component.UiKey))
            return;

        var chargeTarget = chargeRate < 0 ? 0 : component.MaxCharge;
        short chargeEta;
        var atTarget = false;
        if (MathHelper.CloseTo(component.Charge, chargeTarget))
        {
            chargeEta = short.MinValue; // N/A
            atTarget = true;
        }
        else
        {
            var diff = chargeTarget - component.Charge;
            chargeEta = (short) Math.Abs(diff / chargeRate);
        }

        var status = chargeRate switch
        {
            > 0 when atTarget => MachinePowerChargePowerStatus.FullyCharged,
            < 0 when atTarget => MachinePowerChargePowerStatus.Off,
            > 0 => MachinePowerChargePowerStatus.Charging,
            < 0 => MachinePowerChargePowerStatus.Discharging,
            _ => throw new ArgumentOutOfRangeException()
        };

        var state = new ChargingMachineState(
            component.SwitchedOn,
            (byte) (component.Charge * 255),
            status,
            (short) Math.Round(powerReceiver.PowerReceived),
            (short) Math.Round(powerReceiver.Load),
            chargeEta
        );

        _uiSystem.TrySetUiState(
            ent,
            component.UiKey,
            state);

        component.NeedUIUpdate = false;
    }
}
