using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Other.Components;
using Content.Server.Examine;
using Content.Shared.Examine;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Content.Shared.Temperature;

namespace Content.Server.Atmos.Piping.Other.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasMinerSystem : EntitySystem
    {
        [Dependency] private readonly ExamineSystem _examineSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasMinerComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<GasMinerComponent, AtmosDeviceUpdateEvent>(OnMinerUpdated);
        }

        private void OnExamine(EntityUid uid, GasMinerComponent component, ExaminedEvent args)
        {
            using (args.PushGroup(nameof(GasMinerComponent)))
            {
                args.PushText(Loc.GetString("gas-miner-examine-header-text"));

                args.PushText(Loc.GetString("gas-miner-amount-text",
                    ("moles", $"{component.SpawnAmount:0.#}")));

                args.PushText(Loc.GetString("gas-miner-temperature-text",
                    ("tempK", $"{component.SpawnTemperature:0.#}"),
                    ("tempC", $"{TemperatureHelpers.KelvinToCelsius(component.SpawnTemperature):0.#}")));

                if (component.MaxExternalAmount < float.PositiveInfinity)
                {
                    args.PushText(Loc.GetString("gas-miner-moles-cutoff-text",
                        ("moles", $"{component.MaxExternalAmount:0.#}")));
                }

                if (component.MaxExternalPressure < float.PositiveInfinity)
                {
                    args.PushText(Loc.GetString("gas-miner-pressure-cutoff-text",
                        ("pressure", $"{component.MaxExternalPressure:0.#}")));
                }

                if (!component.Enabled || !GetValidEnvironment((uid, component), out var environment))
                {
                    args.PushMarkup(Loc.GetString("gas-miner-state-disabled-text"));
                }
                else if (component.Idle)
                {
                    args.PushMarkup(Loc.GetString("gas-miner-state-idle-text"));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("gas-miner-state-working-text"));
                }
            }
        }

        private void OnMinerUpdated(Entity<GasMinerComponent> ent, ref AtmosDeviceUpdateEvent args)
        {
            var miner = ent.Comp;

            if(!GetValidEnvironment(ent, out var environment))
                return;

            // SpawnAmount is declared in mol/s so to get the amount of gas we hope to mine, we have to multiply this by
            // how long we have been waiting to spawn it and further cap the number according to the miner's state.
            var toSpawn = CapSpawnAmount(ent, miner.SpawnAmount * args.dt, environment);
            miner.Idle = toSpawn == 0;
            if (miner.Idle || !miner.Enabled || !miner.SpawnGas.HasValue)
                return;

            // Time to mine some gas.

            var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas.Value, toSpawn);

            _atmosphereSystem.Merge(environment, merger);
        }

        private bool GetValidEnvironment(Entity<GasMinerComponent> ent, [NotNullWhen(true)] out GasMixture? environment)
        {
            var (uid, miner) = ent;
            var transform = Transform(uid);
            var position = _transformSystem.GetGridOrMapTilePosition(uid, transform);

            // Treat space as an invalid environment
            if (_atmosphereSystem.IsTileSpace(transform.GridUid, transform.MapUid, position))
            {
                environment = null;
                return false;
            }

            environment = _atmosphereSystem.GetContainingMixture((uid, transform), true, true);
            return environment != null;
        }

        private float CapSpawnAmount(Entity<GasMinerComponent> ent, float toSpawnTarget, GasMixture environment)
        {
            var (uid, miner) = ent;

            // How many moles could we theoretically spawn. Cap by pressure and amount.
            var allowableMoles = Math.Min(
                (miner.MaxExternalPressure - environment.Pressure) * environment.Volume / (miner.SpawnTemperature * Atmospherics.R),
                miner.MaxExternalAmount - environment.TotalMoles);

            var toSpawnReal = Math.Clamp(allowableMoles, 0f, toSpawnTarget);

            if (toSpawnReal < Atmospherics.GasMinMoles) {
                return 0f;
            }

            return toSpawnReal;
        }
    }
}
