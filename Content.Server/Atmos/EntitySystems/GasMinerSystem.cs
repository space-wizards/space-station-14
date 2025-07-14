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

    private void ProcessMinerState(Entity<GasMinerComponent> ent, GasMinerState newState, float? possibleNewStoredAmount = null)
    {
        var minerComponent = ent.Comp;

        if (minerComponent.MinerState != newState)
        { minerComponent.MinerState = newState; DirtyField(ent.Owner, minerComponent, nameof(minerComponent.MinerState)); }

        if (possibleNewStoredAmount is { } newStoredAmount && Math.Abs(minerComponent.LastReplicatedStoredAmount - newStoredAmount) >= float.Epsilon)
        {
            minerComponent.StoredAmount = newStoredAmount;
            minerComponent.LastReplicatedStoredAmount = newStoredAmount;
            DirtyField(ent.Owner, minerComponent, nameof(minerComponent.StoredAmount));
        }
    }

    private void OnMinerUpdated(Entity<GasMinerComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        var minerComponent = ent.Comp;

        if (!GetValidEnvironment(ent, out var environment, out var transform, out var minerTilePosition) || !transform.Anchored)
        { ProcessMinerState(ent, GasMinerState.Disabled); return; }

        // The rates of moles mined/released are declared in mol/s, so to get the amount of gas we hope to mine, we have to multiply the rate by
        // how long we have been waiting to spawn it and further cap the number depending on other factors.

        // This is how many mols we can mine right now.
        var molesMinedPerSecond = minerComponent.MiningRate * args.dt;

        // Time to mine some gas. However, only mine as much as we can, and not too much that will exceed the internal storage.
        minerComponent.StoredAmount = Math.Min(minerComponent.MaxStoredAmount, minerComponent.StoredAmount + molesMinedPerSecond);

        // Although we can mine gas in space, it's bad for the environment to release it into space. Atleast like this.
        if (_atmosphereSystem.IsTileSpace(transform.GridUid, transform.MapUid, minerTilePosition))
        { ProcessMinerState(ent, GasMinerState.Idle, possibleNewStoredAmount: minerComponent.StoredAmount); return; }

        // Don't release more gas than is actually stored in the miner.
        float toSpawn = Math.Min(minerComponent.StoredAmount, CapSpawnAmount(ent, minerComponent.ReleaseRate * args.dt, environment));

        if (toSpawn == 0)
        { ProcessMinerState(ent, GasMinerState.Idle, possibleNewStoredAmount: minerComponent.StoredAmount); return; }

        // Release the gas into the atmosphere.
        var merger = new GasMixture(1) { Temperature = minerComponent.SpawnTemperature };
        merger.SetMoles(minerComponent.SpawnGas, toSpawn);
        _atmosphereSystem.Merge(environment, merger);

        minerComponent.StoredAmount -= toSpawn;
        ProcessMinerState(ent, GasMinerState.Working, possibleNewStoredAmount: minerComponent.StoredAmount);
    }

    private bool GetValidEnvironment(Entity<GasMinerComponent> ent, [NotNullWhen(true)] out GasMixture? environment, out TransformComponent transform, out Vector2i tilePosition)
    {
        var uid = ent.Owner;
        transform = Transform(uid);

        tilePosition = _transformSystem.GetGridOrMapTilePosition(uid, transform);
        environment = _atmosphereSystem.GetContainingMixture((uid, transform), true, true);

        return environment != null;
    }

    private float CapSpawnAmount(Entity<GasMinerComponent> ent, float toSpawnTarget, GasMixture environment)
    {
        var minerComponent = ent.Comp;

        // How many moles could we theoretically spawn. Cap by pressure and amount.
        var allowableMoles = Math.Min(
            (minerComponent.MaxExternalPressure - environment.Pressure) * environment.Volume / (minerComponent.SpawnTemperature * Atmospherics.R),
            minerComponent.MaxExternalAmount - environment.TotalMoles);

        var toSpawnReal = Math.Clamp(allowableMoles, 0f, toSpawnTarget);
        return toSpawnReal < Atmospherics.GasMinMoles ? 0f : toSpawnReal;
    }
}
