using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Audio;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class GasReyclerSystem : EntitySystem
    {
        [Dependency] private AppearanceSystem _appearance = default!;
        [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private NodeContainerSystem _nodeContainer = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasRecyclerComponent, AtmosDeviceEnabledEvent>(OnEnabled);
            SubscribeLocalEvent<GasRecyclerComponent, AtmosDeviceUpdateEvent>(OnUpdate);
            SubscribeLocalEvent<GasRecyclerComponent, AtmosDeviceDisabledEvent>(OnDisabled);
            SubscribeLocalEvent<GasRecyclerComponent, ExaminedEvent>(OnExamined);
        }

        private void OnEnabled(EntityUid uid, GasRecyclerComponent comp, ref AtmosDeviceEnabledEvent args)
        {
            UpdateAppearance(uid, comp);
        }

        private void OnExamined(Entity<GasRecyclerComponent> ent, ref ExaminedEvent args)
        {
            var comp = ent.Comp;
            if (!Comp<TransformComponent>(ent).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
                return;

            if (!_nodeContainer.TryGetNode(ent.Owner, comp.InletName, out PipeNode? inlet))
                return;

            using (args.PushGroup(nameof(GasRecyclerComponent)))
            {
                if (comp.Reacting)
                {
                    args.PushMarkup(Loc.GetString("gas-recycler-reacting"));
                }
                else
                {
                    if (inlet.Air.Pressure < comp.MinPressure)
                    {
                        args.PushMarkup(Loc.GetString("gas-recycler-low-pressure"));
                    }

                    if (inlet.Air.Temperature < comp.MinTemp)
                    {
                        args.PushMarkup(Loc.GetString("gas-recycler-low-temperature"));
                    }
                }
            }
        }

        private void OnUpdate(Entity<GasRecyclerComponent> ent, ref AtmosDeviceUpdateEvent args)
        {
            var comp = ent.Comp;
            if (!_nodeContainer.TryGetNodes(ent.Owner, comp.InletName, comp.OutletName, out PipeNode? inlet, out PipeNode? outlet))
            {
                _ambientSoundSystem.SetAmbience(ent, false);
                return;
            }

            // The gas recycler is a passive device, so it permits gas flow even if nothing is being reacted.
            comp.Reacting = inlet.Air.Temperature >= comp.MinTemp && inlet.Air.Pressure >= comp.MinPressure;
            var removed = inlet.Air.RemoveVolume(PassiveTransferVol(inlet.Air, outlet.Air, args.dt));
            if (comp.Reacting)
            {
                var nCO2 = removed.GetMoles(Gas.CarbonDioxide);
                removed.AdjustMoles(Gas.CarbonDioxide, -nCO2);
                removed.AdjustMoles(Gas.Oxygen, nCO2);
                var nN2O = removed.GetMoles(Gas.NitrousOxide);
                removed.AdjustMoles(Gas.NitrousOxide, -nN2O);
                removed.AdjustMoles(Gas.Nitrogen, nN2O);
            }

            _atmosphereSystem.Merge(outlet.Air, removed);
            UpdateAppearance(ent, comp);
            _ambientSoundSystem.SetAmbience(ent, true);
        }

        public float PassiveTransferVol(GasMixture inlet, GasMixture outlet, float dt)
        {
            var dP = inlet.Pressure - outlet.Pressure;
            if (dt <= 0f || dP <= 0f || inlet.Volume <= 0f)
                return 0;

            // recyclers are passive, so they cannot create more pressure in their outlet than the inlet
            // calculate how much we can be allowed to transfer to reach that point and clamp transfer later
            var passiveRatio = Math.Clamp(_atmosphereSystem.FractionToEqualizePressure(inlet, outlet), 0f, 1f);
            var maxTransfer = inlet.Volume * passiveRatio;
            // ensure transfer can occur
            if (maxTransfer <= 0f || float.IsNaN(maxTransfer))
                return 0;

            var overPressConst = 300f; // pressure difference (in atm) to get 200 L/sec transfer rate (assuming PumpSpeedup of 1)
            // multiplying flowRateMultiplier by sqrt(dP) gives the recyclers flow rate
            // however dP changes over time, and Euler's method is too course to give good results here
            var flowRateMultiplier = MathF.Sqrt(1 / (overPressConst * Atmospherics.OneAtmosphere)) * Atmospherics.MaxTransferRate * _atmosphereSystem.PumpSpeedup();

            // if we remove a litre from the inlet, how much does dP change
            // find how many moles are present in a packet of gas volume k taken from the inlet
            // calculate pressure if you remove that many from the inlet and add that many to the outlet
            // take the derivitive with respect to k
            var inletPressureSlope = -inlet.TotalMoles * Atmospherics.R * inlet.Temperature / inlet.Volume / inlet.Volume;
            var outletPressureSlope = -inlet.Pressure * outlet.Temperature / outlet.Volume / inlet.Temperature;
            var pressureSlope = inletPressureSlope + outletPressureSlope;

            // let L(t) be the volume of gas transfered from the inlet
            // let P(t) be the pressure delta at time t
            // L'(t) = c * sqrt(p(t)) where c is the flow rate multiplier
            // P'(t) = s * L'(t) where s is the pressure slope
            // i.e. the flow rate is proportional to the square root of the pressure delta
            // and the rate at which the pressure delta changes is proportional to the flow rate
            // P'(t) = c * s * sqrt(p(t))
            // solve the ODE:
            // P(t) = (2acst + aa + ccsstt)/4 for some constant a
            // P(0) = dP  =>  dP = a^2/4  =>  a = 2 * sqrt(dP)
            // quadratic has exactly one zero at -a/(cs)

            // check if we complete the transfer during this update
            var a = 2 * MathF.Sqrt(dP);
            var interceptTime = -a / flowRateMultiplier / pressureSlope;
            if (dt >= interceptTime)
                return maxTransfer;

            // calculate the desired dP at the end of this using our formula for P(t)
            var cst = flowRateMultiplier * pressureSlope * dt;
            var targetPressureDelta =
                (
                2 * a * cst +
                a * a +
                MathF.Pow(cst, 2)
                ) / 4;

            // use our pressureSlope to find how many litres are needed to achieve this
            var pressureDeltaDelta = dP - targetPressureDelta;
            return -pressureDeltaDelta / pressureSlope;
        }

        private void OnDisabled(EntityUid uid, GasRecyclerComponent comp, ref AtmosDeviceDisabledEvent args)
        {
            comp.Reacting = false;
            UpdateAppearance(uid, comp);
        }

        private void UpdateAppearance(EntityUid uid, GasRecyclerComponent? comp = null)
        {
            if (!Resolve(uid, ref comp, false))
                return;

            _appearance.SetData(uid, PumpVisuals.Enabled, comp.Reacting);
        }
    }
}
