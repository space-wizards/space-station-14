using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Piping.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.EntitySystems;

[UsedImplicitly]
public sealed class GasMinerSystem : SharedGasMinerSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMinerComponent, AtmosDeviceUpdateEvent>(OnMinerUpdated);
    }

    private void OnMinerUpdated(Entity<GasMinerComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        var miner = ent.Comp;
        var oldState = miner.MinerState;
        float toSpawn;

        if (!GetValidEnvironment(ent, out var environment) || !(Transform(ent).Anchored || miner.MineWhileUnanchored))
        {
            miner.MinerState = GasMinerState.Disabled;
        }
        // SpawnAmount is declared in mol/s so to get the amount of gas we hope to mine, we have to multiply this by
        // how long we have been waiting to spawn it and further cap the number according to the miner's state.
        else if ((toSpawn = CapSpawnAmount(ent, miner.SpawnAmount * args.dt, environment)) == 0)
        {
            miner.MinerState = GasMinerState.Idle;
        }
        else
        {
            miner.MinerState = GasMinerState.Working;

            // Time to mine some gas.
            var merger = new GasMixture(1) { Temperature = miner.SpawnTemperature };
            merger.SetMoles(miner.SpawnGas, toSpawn);
            _atmosphereSystem.Merge(environment, merger);
        }

        if (miner.MinerState != oldState)
        {
            Dirty(ent);
        }
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
