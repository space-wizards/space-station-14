using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.Construction;
using Content.Server.Gravity;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Construction;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Power.Generation.EnergyCore;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Content.Shared.Tools.Systems;
using Content.Shared.Tools;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Power.Generation.EnergyCore;

public sealed partial class EnergyCoreSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private GravityGeneratorSystem _gravity = default!;
    [Dependency] private GravitySystem _gridGravity = default!;
    [Dependency] private ThrusterSystem _thrusters = default!;
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private SharedRadiationSystem _radiation = default!;
    [Dependency] private NodeContainerSystem _nodes = default!;
    [Dependency] private SharedMaterialStorageSystem _materials = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;

    [Dependency] private SharedToolSystem _tools = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    private const float MaxSimulationStep = 0.25f;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(PowerNetSystem));
        SubscribeLocalEvent<EnergyCoreComponent, InteractUsingEvent>(OnInteractUsingEvent, before: [typeof(ConstructionSystem)]);
        SubscribeLocalEvent<EnergyCoreComponent, ConstructionInteractDoAfterEvent>(OnConstructionFinishedEvent, before: [typeof(ConstructionSystem)]);
        SubscribeLocalEvent<EnergyCoreComponent, MaterialAmountChangedEvent>(OnMaterialAmountChangedEvent);
        SubscribeLocalEvent<EnergyCoreComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<EnergyCoreComponent, ComponentShutdown>(OnShutdownEvent);
        SubscribeLocalEvent<EnergyCoreComponent, AnchorStateChangedEvent>(OnAnchorChangedEvent);
        SubscribeLocalEvent<EnergyCoreComponent, EntParentChangedMessage>(OnParentChangedEvent);
        SubscribeLocalEvent<EnergyCoreComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdateEvent);
        SubscribeLocalEvent<EnergyCoreComponent, ExaminedEvent>(OnExaminedEvent);
    }

    private bool UnsafeToDismantle(Entity<EnergyCoreComponent> ent)
    {
        return ent.Comp.State != EnergyCoreState.Disabled || ent.Comp.Temperature >= ent.Comp.RestartTemperature;
    }

    private void OnInteractUsingEvent(Entity<EnergyCoreComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tools.HasQuality(args.Used, PryingQuality.Id) || !UnsafeToDismantle(ent))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("energy-core-dismantle-unsafe"), ent, args.User);
    }

    private void OnConstructionFinishedEvent(Entity<EnergyCoreComponent> ent, ref ConstructionInteractDoAfterEvent args)
    {
        if (!args.Cancelled && UnsafeToDismantle(ent))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("energy-core-dismantle-unsafe"), ent, args.User);
        }
    }

    private void OnMapInitEvent(Entity<EnergyCoreComponent> ent, ref MapInitEvent args)
    {
        SetState(ent, EnergyCoreState.Disabled);
    }

    private void OnShutdownEvent(Entity<EnergyCoreComponent> ent, ref ComponentShutdown args)
    {
        SetState(ent, EnergyCoreState.Disabled);
    }

    private void OnAnchorChangedEvent(Entity<EnergyCoreComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            SetState(ent, EnergyCoreState.Disabled);
    }

    private void OnParentChangedEvent(Entity<EnergyCoreComponent> ent, ref EntParentChangedMessage args)
    {
        if (ent.Comp.State == EnergyCoreState.Disabled)
            return;

        SetState(ent, EnergyCoreState.Disabled);
        if (TryComp<GravityComponent>(args.OldParent, out var gravity))
            _gridGravity.RefreshGravity(args.OldParent.Value, gravity);
    }

    private void OnExaminedEvent(Entity<EnergyCoreComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("energy-core-examine",
                ("temperature", ent.Comp.Temperature), ("power", ent.Comp.TargetSupply / 1000)));
    }

    private void OnMaterialAmountChangedEvent(Entity<EnergyCoreComponent> ent, ref MaterialAmountChangedEvent args)
    {
        UpdateRadiation(ent);
    }

    private void UpdateRadiation(Entity<EnergyCoreComponent> ent)
    {
        var hasFuel = AvailableFuel(ent) > 0;
        var intensity = hasFuel ? ent.Comp.IdleRadiation : 0;
        if (ent.Comp.MaxSupply > 0)
            intensity += ent.Comp.ActiveRadiation *
                (ent.Comp.FissionPower + ent.Comp.DecayEnergy / (float) ent.Comp.DecayTime.TotalSeconds) *
                ent.Comp.NominalEfficiency / ent.Comp.MaxSupply;

        _radiation.SetIntensity(ent.Owner, intensity);
    }

    public void SetOutput(Entity<EnergyCoreComponent?> ent, float output, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp) || !float.IsFinite(output))
            return;

        output = Math.Clamp(output, 0, ent.Comp.MaxSupply);
        if (ent.Comp.TargetSupply == output)
            return;

        ent.Comp.TargetSupply = output;
        if (user != null)
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(user.Value):player} set {ToPrettyString(ent)} output to {output} W");
        UpdateSupply((ent.Owner, ent.Comp));
        UpdateRadiation((ent.Owner, ent.Comp));
    }

    public void SetEnabled(Entity<EnergyCoreComponent?> ent, bool enabled, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (enabled)
        {
            if (ent.Comp.State != EnergyCoreState.Disabled || !CanStart((ent.Owner, ent.Comp)))
                return;

            ent.Comp.TransitionEnd = _timing.CurTime + ent.Comp.StartupTime;
            SetState((ent.Owner, ent.Comp), EnergyCoreState.Starting);
        }
        else
        {
            if (ent.Comp.State is EnergyCoreState.Disabled or EnergyCoreState.Stopping)
                return;

            ent.Comp.TransitionEnd = _timing.CurTime + ent.Comp.ShutdownTime;
            SetState((ent.Owner, ent.Comp), EnergyCoreState.Stopping);
        }

        if (user != null)
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(user.Value):player} set {ToPrettyString(ent)} enabled to {enabled}");
    }

    private float AvailableFuel(Entity<EnergyCoreComponent> ent)
    {
        var stored = TryComp<MaterialStorageComponent>(ent, out var storage)
            ? _materials.GetMaterialAmount(ent, ent.Comp.FuelMaterial.Id, storage, localOnly: true) : 0;
        return ent.Comp.FuelEnergyRemaining + ent.Comp.FuelEnergyPerUnit * stored;
    }

    private bool CanStart(Entity<EnergyCoreComponent> ent)
    {
        var xform = Transform(ent);
        return xform.Anchored && xform.GridUid != null && AvailableFuel(ent) > 0 &&
               ent.Comp.Temperature < ent.Comp.RestartTemperature;
    }

    private void UpdateSupply(Entity<EnergyCoreComponent> ent)
    {
        if (!TryComp<PowerSupplierComponent>(ent, out var supplier))
            return;

        supplier.Enabled = ent.Comp.State == EnergyCoreState.Enabled;
        supplier.MaxSupply = supplier.Enabled ? Math.Min(supplier.MaxSupply, ent.Comp.TargetSupply) : 0;
    }

    private void SetState(Entity<EnergyCoreComponent> ent, EnergyCoreState state)
    {
        ent.Comp.State = state;
        var running = state == EnergyCoreState.Enabled;
        if (!running)
        {
            ent.Comp.FissionPower = 0;
            ent.Comp.Temperature += ent.Comp.ElectricalEnergy / ent.Comp.HeatCapacity;
            ent.Comp.ElectricalEnergy = 0;
            ent.Comp.ConversionEfficiency = 0;
        }
        UpdateSupply(ent);
        if (TryComp<GravityGeneratorComponent>(ent, out var gravity))
            _gravity.SetActive((ent, gravity), running);
        if (TryComp<ThrusterComponent>(ent, out var thruster))
            _thrusters.SetEnabled((ent, thruster), running);
        _ambient.SetAmbience(ent, running);
        _appearance.SetData(ent, EnergyCoreVisuals.State, state);
        UpdateRadiation(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<EnergyCoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Paused(uid))
                continue;

            var ent = new Entity<EnergyCoreComponent>(uid, comp);
            if (comp.State is EnergyCoreState.Starting or EnergyCoreState.Stopping &&
                _timing.CurTime >= comp.TransitionEnd)
                SetState(ent, comp.State == EnergyCoreState.Starting && CanStart(ent)
                    ? EnergyCoreState.Enabled : EnergyCoreState.Disabled);

            PreparePower(ent, frameTime);
        }
    }

    private void PreparePower(Entity<EnergyCoreComponent> ent, float frameTime)
    {
        if (frameTime <= 0 || !float.IsFinite(frameTime))
            return;

        var comp = ent.Comp;
        comp.HeatOutput = 0;
        comp.DecayHeat = 0;
        var reactionTime = (float) comp.ReactionTime.TotalSeconds;
        var decayTime = (float) comp.DecayTime.TotalSeconds;
        var remaining = frameTime;
        while (remaining > 0)
        {
            var dt = Math.Min(remaining, MaxSimulationStep);
            remaining -= dt;
            var fuel = 0f;
            if (comp.State == EnergyCoreState.Enabled)
            {
                var feedback = 1 / (1 + comp.TemperatureFeedback * Math.Max(0, comp.Temperature - comp.FeedbackTemperature));
                var target = Math.Max(comp.TargetSupply, comp.MinimumOperatingSupply) / comp.NominalEfficiency * feedback;
                var response = MathF.Exp(-dt / reactionTime);
                var requested = target * dt + (comp.FissionPower - target) * reactionTime * (1 - response);
                fuel = ConsumeFuel(ent, Math.Max(0, requested));
                comp.FissionPower = (target + (comp.FissionPower - target) * response) *
                                    (requested > 0 ? fuel / requested : 0);
            }

            // dE_decay/dt = fraction * P_fission - E_decay/tau.
            var decay = MathF.Exp(-dt / decayTime);
            var stored = comp.DecayEnergy * decay + comp.DecayFraction * fuel * decayTime / dt * (1 - decay);
            var released = Math.Max(0, comp.DecayEnergy + comp.DecayFraction * fuel - stored);
            comp.DecayEnergy = stored;
            var heat = fuel * (1 - comp.DecayFraction) + released;
            comp.Temperature += heat / comp.HeatCapacity;
            comp.HeatOutput += heat / frameTime;
            comp.DecayHeat += released / frameTime;
            if (comp.State == EnergyCoreState.Enabled &&
                (AvailableFuel(ent) <= 0 || comp.Temperature >= comp.ShutdownTemperature))
                SetEnabled((ent.Owner, comp), false);
        }

        var power = comp.State == EnergyCoreState.Enabled
            ? Math.Min(comp.TargetSupply, comp.ElectricalEnergy / frameTime) : 0;
        comp.PendingElectricalEnergy = power * frameTime;
        comp.ElectricalEnergy -= comp.PendingElectricalEnergy;
        if (TryComp<PowerSupplierComponent>(ent, out var supplier))
            supplier.MaxSupply = power;
        UpdateRadiation(ent);
    }

    public void FinishPowerUpdate(float frameTime)
    {
        if (frameTime <= 0 || !float.IsFinite(frameTime))
            return;

        var query = EntityQueryEnumerator<EnergyCoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Paused(uid))
                continue;

            var exported = TryComp<PowerSupplierComponent>(uid, out var supplier)
                ? supplier.CurrentSupply * frameTime : 0;
            var unused = Math.Max(0, comp.PendingElectricalEnergy - exported);
            comp.PendingElectricalEnergy = 0;
            var retained = comp.State == EnergyCoreState.Enabled
                ? Math.Min(unused, Math.Max(0, comp.ElectricalCapacity - comp.ElectricalEnergy)) : 0;
            comp.ElectricalEnergy += retained;
            comp.Temperature += (unused - retained) / comp.HeatCapacity;
            if (comp.Temperature >= comp.ShutdownTemperature)
                SetEnabled((uid, comp), false);
        }
    }

    private void OnAtmosUpdateEvent(Entity<EnergyCoreComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (Paused(ent.Owner) || !Transform(ent).Anchored || args.dt <= 0 || !float.IsFinite(args.dt))
            return;

        var comp = ent.Comp;
        comp.CoolantFlow = 0;
        comp.ConversionEfficiency = 0;
        var transferredHeat = 0f;
        var generatedEnergy = 0f;
        var remaining = args.dt;
        while (remaining > 0)
        {
            var dt = Math.Min(remaining, MaxSimulationStep);
            remaining -= dt;
            var temperature = comp.Temperature;
            var energy = comp.ElectricalEnergy;
            comp.CoolantFlow += Circulate(ent, dt) / args.dt;
            transferredHeat += Math.Max(0, (temperature - comp.Temperature) * comp.HeatCapacity);
            generatedEnergy += Math.Max(0, comp.ElectricalEnergy - energy);
            if (comp.Temperature >= comp.ShutdownTemperature)
                SetEnabled((ent.Owner, comp), false);
        }
        comp.ConversionEfficiency = transferredHeat > 0 ? generatedEnergy / transferredHeat : 0;
        UpdateRadiation(ent);
    }

    private float ConsumeFuel(Entity<EnergyCoreComponent> ent, float requested)
    {
        var comp = ent.Comp;
        if (requested > comp.FuelEnergyRemaining)
        {
            var stored = _materials.GetMaterialAmount(ent, comp.FuelMaterial.Id, localOnly: true);
            var units = (int) Math.Min(stored, Math.Ceiling((requested - comp.FuelEnergyRemaining) / comp.FuelEnergyPerUnit));
            if (units > 0 && _materials.TryChangeMaterialAmount(ent, comp.FuelMaterial.Id, -units, localOnly: true))
                comp.FuelEnergyRemaining += units * comp.FuelEnergyPerUnit;
        }

        var consumed = Math.Min(requested, comp.FuelEnergyRemaining);
        comp.FuelEnergyRemaining -= consumed;
        return consumed;
    }

    private float Circulate(Entity<EnergyCoreComponent> ent, float dt)
    {
        var comp = ent.Comp;
        if (!_nodes.TryGetNodes(ent.Owner, comp.Inlet, comp.Outlet, out PipeNode? inlet, out PipeNode? outlet))
            return 0;

        var input = inlet.Air;
        var output = outlet.Air;
        if (input.Immutable || output.Immutable || input.Volume <= 0 || output.Volume <= 0 || input.TotalMoles <= 0 ||
            output.Pressure - input.Pressure >= comp.PressureHead)
            return 0;

        var ratio = Math.Clamp(comp.FlowRate * _atmosphere.PumpSpeedup() * dt / input.Volume, 0, 1);
        if (!ReferenceEquals(input, output))
            ratio = LimitTransferRatio(comp, input, output, ratio, dt);
        if (ratio <= 0)
            return 0;

        var coolant = input.RemoveRatio(ratio);
        ExchangeHeat((ent.Owner, comp), coolant, dt);
        var moles = coolant.TotalMoles;
        MergeCoolant(output, coolant);
        return moles;
    }

    private void MergeCoolant(GasMixture receiver, GasMixture coolant)
    {
        if (coolant.TotalMoles <= 0)
            return;

        var receiverCapacity = receiver.TotalMoles > 0 ? _atmosphere.GetHeatCapacity(receiver, true) : 0;
        var coolantCapacity = _atmosphere.GetHeatCapacity(coolant, true);
        var temperature = ((double) receiver.Temperature * receiverCapacity +
                           (double) coolant.Temperature * coolantCapacity) / (receiverCapacity + coolantCapacity);
        _atmosphere.Merge(receiver, coolant);
        receiver.Temperature = (float) temperature;
    }

    private float LimitTransferRatio(EnergyCoreComponent comp, GasMixture input, GasMixture output, float ratio, float dt)
    {
        var trialInput = input.Clone();
        var trialOutput = output.Clone();
        var lower = 0f;
        var upper = ratio;
        for (var i = 0; i < 16; i++)
        {
            trialInput.CopyFrom(input);
            trialOutput.CopyFrom(output);
            var coolant = trialInput.RemoveRatio(ratio);
            var capacity = _atmosphere.GetHeatCapacity(coolant, true);
            var heat = GetExchangedHeat(comp, coolant, dt);
            coolant.Temperature += (heat - GetGeneratedEnergy(comp, coolant, heat, dt)) / capacity;
            MergeCoolant(trialOutput, coolant);
            if (trialOutput.Pressure - trialInput.Pressure <= comp.PressureHead)
            {
                lower = ratio;
                if (i == 0)
                    return ratio;
            }
            else
                upper = ratio;
            ratio = (lower + upper) / 2;
        }
        return lower;
    }

    private float GetExchangedHeat(EnergyCoreComponent comp, GasMixture coolant, float dt)
    {
        if (coolant.TotalMoles <= 0)
            return 0;

        var gasCapacity = _atmosphere.GetHeatCapacity(coolant, true);
        var reducedCapacity = comp.HeatCapacity * gasCapacity / (comp.HeatCapacity + gasCapacity);
        var opening = comp.State == EnergyCoreState.Enabled
            ? Math.Clamp(Math.Max(comp.TargetSupply, comp.MinimumOperatingSupply) / comp.MaxSupply, 0, 1) : 1;
        return (comp.Temperature - coolant.Temperature) * reducedCapacity *
               (1 - MathF.Exp(-comp.Conductance * opening * dt / reducedCapacity));
    }

    private float GetGeneratedEnergy(EnergyCoreComponent comp, GasMixture coolant, float heat, float dt)
    {
        if (comp.State != EnergyCoreState.Enabled || heat <= 0 || comp.TargetSupply <= 0)
            return 0;

        // Use the coldest hot-side and hottest cold-side temperatures over this step.
        // This is a conservative Carnot bound even for finite gas/core heat capacities.
        var hot = comp.Temperature - heat / comp.HeatCapacity;
        var cold = coolant.Temperature + heat / _atmosphere.GetHeatCapacity(coolant, true);
        var efficiency = comp.CycleEfficiency * Math.Clamp(1 - cold / hot, 0, 1);
        return Math.Min(heat * efficiency, Math.Min(comp.TargetSupply * dt,
            Math.Max(0, comp.ElectricalCapacity - comp.ElectricalEnergy - comp.PendingElectricalEnergy)));
    }

    public void ExchangeHeat(Entity<EnergyCoreComponent?> ent, GasMixture coolant, float dt)
    {
        if (!Resolve(ent, ref ent.Comp) || coolant.Immutable || coolant.TotalMoles <= 0 || dt <= 0 || !float.IsFinite(dt))
            return;

        var gasCapacity = _atmosphere.GetHeatCapacity(coolant, true);
        var heat = GetExchangedHeat(ent.Comp, coolant, dt);
        var generated = GetGeneratedEnergy(ent.Comp, coolant, heat, dt);
        ent.Comp.Temperature -= heat / ent.Comp.HeatCapacity;
        coolant.Temperature += (heat - generated) / gasCapacity;
        ent.Comp.ElectricalEnergy += generated;
    }

    public EnergyCoreUiState GetUiState(Entity<EnergyCoreComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return new EnergyCoreUiState(false);

        var supply = TryComp<PowerSupplierComponent>(ent, out var supplier) ? supplier.CurrentSupply : 0;
        _nodes.TryGetNodes(ent.Owner, ent.Comp.Inlet, ent.Comp.Outlet, out PipeNode? inlet, out PipeNode? outlet);
        var comp = ent.Comp;
        var radiation = TryComp<RadiationSourceComponent>(ent, out var source) ? source.Intensity : 0;
        return new EnergyCoreUiState(true, comp.State, AvailableFuel((ent.Owner, comp)),
            comp.Temperature, comp.RestartTemperature, supply, comp.TargetSupply, comp.MaxSupply,
            comp.HeatOutput, comp.CoolantFlow, inlet?.Air.Temperature ?? 0, outlet?.Air.Temperature ?? 0,
            inlet?.Air.Pressure ?? 0, outlet?.Air.Pressure ?? 0, radiation,
            comp.FissionPower, comp.DecayHeat, comp.ConversionEfficiency, comp.ElectricalEnergy);
    }
}
