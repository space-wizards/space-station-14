using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.Machines.EntitySystems;
using Content.Shared.DeviceNetwork.Systems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Content.Shared.Machines.Components;
using Content.Shared.Machines.EntitySystems;
using Content.Shared.Machines.Events;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Generation.Teg;
using Content.Shared.Rounding;
using Robust.Server.GameObjects;

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// Handles processing logic for the thermo-electric generator (TEG).
/// </summary>
/// <remarks>
/// <para>
/// The TEG generates power by exchanging heat between gases flowing through its two sides.
/// The gas flows through a "circulator" entity on each side, which have both an inlet and an outlet port.
/// </para>
/// <remarks>
/// Connecting the TEG core to its circulators is implemented via <see cref="MultipartMachineComponent"/>.
/// </remarks>
/// <para>
/// The TEG center does HV power output, and must also be connected to an LV wire for the TEG to function.
/// </para>
/// <para>
/// Unlike in SS13, the TEG actually adjusts gas heat exchange to match the energy demand of the power network.
/// To achieve this, the TEG implements its own ramping logic instead of using the built-in Pow3r ramping.
/// The TEG actually has a maximum output of +n% more than was really generated,
/// which allows Pow3r to draw more power to "signal" that there is more network load.
/// The ramping is also exponential instead of linear like in normal Pow3r.
/// This system does mean a fully-loaded TEG creates +n% power out of thin air, but this is considered acceptable.
/// </para>
/// </remarks>
/// <seealso cref="TegGeneratorComponent"/>
/// <seealso cref="TegCirculatorComponent"/>
/// <seealso cref="TegSensorPayload"/>
public sealed partial class TegSystem : EntitySystem
{
    /// <summary>
    /// Node name for the inlet pipe of a circulator.
    /// </summary>
    private const string NodeNameInlet = "inlet";

    /// <summary>
    /// Node name for the outlet pipe of a circulator.
    /// </summary>
    private const string NodeNameOutlet = "outlet";

    [Dependency] private AmbientSoundSystem _ambientSound = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private SharedMultipartMachineSystem _multipart = default!;

    [Dependency] private EntityQuery<NodeContainerComponent> _nodeContainerQuery;
    [Dependency] private EntityQuery<MultipartMachineComponent> _multipartQuery;

    [SubscribeLocalEvent]
    private void GeneratorExamined(EntityUid uid, TegGeneratorComponent component, ref ExaminedEvent args)
    {
        if (!_multipart.IsAssembled(uid))
        {
            args.PushMarkup(Loc.GetString("teg-generator-examine-connection"));
        }
        else
        {
            var supplier = Comp<PowerSupplierComponent>(uid);

            using (args.PushGroup(nameof(TegGeneratorComponent)))
            {
                args.PushMarkup(Loc.GetString("teg-generator-examine-power", ("power", supplier.CurrentSupply)));
                args.PushMarkup(Loc.GetString("teg-generator-examine-power-max-output", ("power", supplier.MaxSupply)));
            }
        }
    }

    [SubscribeLocalEvent]
    private void GeneratorUpdate(EntityUid uid, TegGeneratorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);
        var powerReceiver = Comp<ApcPowerReceiverComponent>(uid);
        if (!powerReceiver.Powered)
        {
            supplier.MaxSupply = 0;
            return;
        }

        if (!_multipartQuery.TryComp(uid, out var multipartComp)
            || !_multipart.IsAssembled((uid, multipartComp)))
            return;

        if (!_multipart.TryGetPartEntity((uid, multipartComp), TegParts.CirculatorA, out var circA)
            || !_multipart.TryGetPartEntity((uid, multipartComp), TegParts.CirculatorB, out var circB))
            return;

        var (inletA, outletA) = GetPipes(circA.Value);
        var (inletB, outletB) = GetPipes(circB.Value);

        var (airA, δpA) = GetCirculatorAirTransfer(inletA.Air, outletA.Air);
        var (airB, δpB) = GetCirculatorAirTransfer(inletB.Air, outletB.Air);

        var cA = _atmosphere.GetHeatCapacity(airA, true);
        var cB = _atmosphere.GetHeatCapacity(airB, true);

        // Shift ramp position based on demand and generation from previous tick.
        var curRamp = component.RampPosition;
        var lastDraw = supplier.CurrentSupply;
        curRamp = MathHelper.Clamp(lastDraw, curRamp / component.RampFactor, curRamp * component.RampFactor);
        curRamp = MathF.Max(curRamp, component.RampMinimum);
        component.RampPosition = curRamp;

        var electricalEnergy = 0f;

        if (airA.Pressure > 0 && airB.Pressure > 0)
        {
            var hotA = airA.Temperature > airB.Temperature;

            // Calculate thermal and electrical energy transfer between the two sides.
            // Assume temperature equalizes, i.e. Ta*cA + Tb*cB = Tf*(cA+cB)
            var Tf = (airA.Temperature * cA + airB.Temperature * cB) / (cA + cB);
            // The maximum energy we can extract is (Ta - Tf)*cA, which is equal to (Tf - Tb)*cB
            var Wmax = MathF.Abs(airA.Temperature - Tf) * cA;

            var N = component.ThermalEfficiency;

            // Calculate Carnot efficiency
            var Thot = hotA ? airA.Temperature : airB.Temperature;
            var Tcold = hotA ? airB.Temperature : airA.Temperature;
            var Nmax = 1 - Tcold / Thot;
            N = MathF.Min(N, Nmax); // clamp by Carnot efficiency

            // Reduce efficiency at low temperature differences to encourage burn chambers (instead
            // of just feeding the TEG room temperature gas from an infinite gas miner).
            var dT = Thot - Tcold;
            N *= MathF.Tanh(dT/700); // https://www.wolframalpha.com/input?i=tanh(x/700)+from+0+to+1000

            var transfer = Wmax * N;
            electricalEnergy = transfer * component.PowerFactor;
            var outTransfer = transfer * (1 - component.ThermalEfficiency);

            // Adjust thermal energy in transferred gas mixtures.
            if (hotA)
            {
                // A -> B
                airA.Temperature -= transfer / cA;
                airB.Temperature += outTransfer / cB;
            }
            else
            {
                // B -> A
                airA.Temperature += outTransfer / cA;
                airB.Temperature -= transfer / cB;
            }
        }

        component.LastGeneration = electricalEnergy;

        // Turn energy (at atmos tick rate) into wattage.
        var power = electricalEnergy / args.dt;

        // Add ramp factor. This magics slight power into existence, but allows us to ramp up.
        // Also apply an exponential moving average to smooth out fluttering, as it was causing
        // seizures.
        supplier.MaxSupply = component.PowerSmoothingFactor * (power * component.RampFactor) +
                             (1 - component.PowerSmoothingFactor) * supplier.MaxSupply;

        var circAComp = Comp<TegCirculatorComponent>(circA.Value);
        var circBComp = Comp<TegCirculatorComponent>(circB.Value);

        circAComp.LastPressureDelta = δpA;
        circAComp.LastMolesTransferred = airA.TotalMoles;
        circBComp.LastPressureDelta = δpB;
        circBComp.LastMolesTransferred = airB.TotalMoles;

        _atmosphere.Merge(outletA.Air, airA);
        _atmosphere.Merge(outletB.Air, airB);

        UpdateAppearance(uid, component, powerReceiver);
    }

    [SubscribeLocalEvent(before: [typeof(MultipartMachineSystem)])]
    private void OnTegStartup(Entity<TegGeneratorComponent> ent, ref ComponentStartup args)
    {
        _receiver.SetPowerDisabled(ent.Owner, true);
    }

    [SubscribeLocalEvent]
    private void OnMultipartChanged(Entity<TegGeneratorComponent> ent, ref MultipartMachineAssemblyStateChanged args)
    {
        var powerReceiver = Comp<ApcPowerReceiverComponent>(ent.Owner);
        _receiver.SetPowerDisabled(ent.Owner, !args.IsAssembled, powerReceiver);
        UpdateAppearance(ent.Owner, ent.Comp, powerReceiver);

        foreach (var circ in args.PartsAdded.Values)
        {
            UpdateCirculatorAppearance(circ, args.IsAssembled);
        }

        foreach (var circ in args.PartsRemoved.Values)
        {
            UpdateCirculatorAppearance(circ, false);
        }
    }

    private void UpdateAppearance(
        EntityUid uid,
        TegGeneratorComponent component,
        ApcPowerReceiverComponent powerReceiver)
    {
        int powerLevel;
        if (powerReceiver.Powered)
        {
            powerLevel = ContentHelpers.RoundToLevels(
                component.RampPosition - component.RampMinimum,
                component.MaxVisualPower - component.RampMinimum,
                12);
        }
        else
        {
            powerLevel = 0;
        }

        _ambientSound.SetAmbience(uid, powerLevel >= 1);
        // TODO: Ok so this introduces popping which is a major shame big rip.
        // _ambientSound.SetVolume(uid, MathHelper.Lerp(component.VolumeMin, component.VolumeMax, MathHelper.Clamp01(component.RampPosition / component.MaxVisualPower)));

        _appearance.SetData(uid, TegVisuals.PowerOutput, powerLevel);

        if (!_multipartQuery.TryComp(uid, out var multipartComp)
            || !_multipart.IsAssembled((uid, multipartComp)))
            return;

        if (_multipart.TryGetPartEntity((uid, multipartComp), TegParts.CirculatorA, out var circA))
            UpdateCirculatorAppearance(circA.Value, powerReceiver.Powered);
        if (_multipart.TryGetPartEntity((uid, multipartComp), TegParts.CirculatorB, out var circB))
            UpdateCirculatorAppearance(circB.Value, powerReceiver.Powered);
    }

    private void UpdateCirculatorAppearance(Entity<TegCirculatorComponent?> ent, bool powered)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var circ = ent.Comp;

        TegCirculatorSpeed speed;
        if (powered && circ.LastPressureDelta > 0 && circ.LastMolesTransferred > 0)
        {
            if (circ.LastPressureDelta > circ.VisualSpeedDelta)
                speed = TegCirculatorSpeed.SpeedFast;
            else
                speed = TegCirculatorSpeed.SpeedSlow;
        }
        else
        {
            speed = TegCirculatorSpeed.SpeedStill;
        }

        _appearance.SetData(ent, TegVisuals.CirculatorSpeed, speed);
        _appearance.SetData(ent, TegVisuals.CirculatorPower, powered);

        if (_pointLight.TryGetLight(ent, out var pointLight))
        {
            _pointLight.SetEnabled(ent, powered, pointLight);
            _pointLight.SetColor(ent, speed == TegCirculatorSpeed.SpeedFast ? circ.LightColorFast : circ.LightColorSlow, pointLight);
        }
    }

    [SubscribeLocalEvent]
    private void GeneratorPowerChange(EntityUid uid, TegGeneratorComponent component, ref PowerChangedEvent args)
    {
        // TODO: I wish power events didn't go out on shutdown.
        if (TerminatingOrDeleted(uid))
            return;

        UpdateAppearance(uid, component, Comp<ApcPowerReceiverComponent>(uid));
    }

    private static (GasMixture, float δp) GetCirculatorAirTransfer(GasMixture airInlet, GasMixture airOutlet)
    {
        var n1 = airInlet.TotalMoles;
        var n2 = airOutlet.TotalMoles;
        var p1 = airInlet.Pressure;
        var p2 = airOutlet.Pressure;
        var V1 = airInlet.Volume;
        var V2 = airOutlet.Volume;
        var T1 = airInlet.Temperature;
        var T2 = airOutlet.Temperature;

        var δp = p1 - p2;

        var denom = T1 * V2 + T2 * V1;

        if (δp > 0 && p1 > 0 && denom > 0)
        {
            var transferMoles = n1 - (n1 + n2) * T2 * V1 / denom;
            return (airInlet.Remove(transferMoles), δp);
        }

        return (new GasMixture(), δp);
    }

    private (PipeNode inlet, PipeNode outlet) GetPipes(EntityUid uidCirculator)
    {
        var nodeContainer = _nodeContainerQuery.GetComponent(uidCirculator);
        var inlet = (PipeNode) nodeContainer.Nodes[NodeNameInlet];
        var outlet = (PipeNode) nodeContainer.Nodes[NodeNameOutlet];

        return (inlet, outlet);
    }

    [SubscribeLocalEvent]
    private void OnSyncPayload(Entity<TegGeneratorComponent> ent, ref DeviceNetworkPacketEvent<TegSensorSyncPayload> args)
    {
        if (!_multipartQuery.TryComp(ent.Owner, out var multipartComp)
            || !_multipart.IsAssembled((ent.Owner, multipartComp)))
            return;

        var supplier = Comp<PowerSupplierComponent>(ent);

        var dataPayload = new TegSensorPayload
        {
            CirculatorA = GetCirculatorSensorData(_multipart.GetPartEntity((ent.Owner, multipartComp), TegParts.CirculatorA)!.Value),
            CirculatorB = GetCirculatorSensorData(_multipart.GetPartEntity((ent.Owner, multipartComp), TegParts.CirculatorB)!.Value),
            LastGeneration = ent.Comp.LastGeneration,
            PowerOutput = supplier.CurrentSupply,
            RampPosition = ent.Comp.RampPosition,
        };

        _deviceNetwork.SendPacket(ent.Owner, args.SenderAddress, ref dataPayload);
    }

    private Circulator GetCirculatorSensorData(EntityUid circulator)
    {
        var (inlet, outlet) = GetPipes(circulator);

        return new Circulator(
            inlet.Air.Pressure,
            outlet.Air.Pressure,
            inlet.Air.Temperature,
            outlet.Air.Temperature);
    }
}
