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

            // step 1: passive equalization budget, must not exceed
            var passiveRatio = Math.Clamp(_atmosphereSystem.FractionToEqualizePressure(inlet, outlet), 0f, 1f);
            var affected = inlet.Volume * passiveRatio;
            if (affected <= 0f) // also reject bogus budgets, need only ratio
                return 0;

            // step 2: naive gas recycler flow
            // unlike linear devices gas recycler follows sqrt law, so model differently from base PassiveTransferVol
            // to eventually integrate

            float overPressConst = 300f; // pressure difference (in atm) to get 200 L/sec transfer rate
                                         // old alpha = maxFlow / sqrt(scale), hence flowRate = alpha * sqrt(dP)
            var flowRate = Atmospherics.MaxTransferRate * _atmosphereSystem.PumpSpeedup() * (float)Math.Sqrt(dP / (overPressConst * Atmospherics.OneAtmosphere));
            var naiveRatio = flowRate / inlet.Volume;

            // step 3: math jumpscare
            // integrate df/dt = naiveRatio * (1 - f) * sqrt(1 - f / passiveRatio); where f = removedRatio
            // assume dP falling linearly with f, (1-f) accounts for density
            // sub u = sqrt(1 - f / passiveRatio)
            var beta = (1f - passiveRatio) / passiveRatio;

            var h = 0.5f * naiveRatio * dt;
            if (beta == 0f) // early out or we end up infecting with nonfinites, bad
            {
                var z = h >= 1f // true branch: handle infinities at the source more expensively
                    ? 1f / (1f + 1f / h)
                    : h / (1f + h);
                return inlet.Volume * (z * (2f - z));
            }

            var rtBeta = MathF.Sqrt(beta);
            var x = rtBeta * h;

            // equilibrium reached in finite time for beta > 0
            if (x >= MathF.Atan2(1f, rtBeta))
                return inlet.Volume * passiveRatio;

            var cX = MathF.Cos(x);
            var y = h * (x == 0f ? 1f : MathF.Sin(x) / x);

            //var u = (cX - beta * y) / (cX + y);
            //but account for low dt
            var denom = cX + y;
            var removedRatio =
                y * (2f * cX + (1f - beta) * y)
                / (denom * denom);

            return inlet.Volume * Math.Clamp(removedRatio, 0f, passiveRatio);
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
