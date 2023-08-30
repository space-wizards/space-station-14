using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

/// <summary>
/// This handles activation upon certain pressure thresholds.
/// </summary>
public sealed class ArtifactPressureTriggerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<ArtifactComponent> toUpdate = new();
        foreach (var (trigger, artifact, transform) in EntityQuery<ArtifactPressureTriggerComponent, ArtifactComponent, TransformComponent>())
        {
            var uid = trigger.Owner;
            var environment = _atmosphereSystem.GetTileMixture(transform.GridUid, transform.MapUid,
                _transformSystem.GetGridOrMapTilePosition(uid, transform));

            if (environment == null)
                continue;

            var pressure = environment.Pressure;
            if (pressure >= trigger.MaxPressureThreshold || pressure <= trigger.MinPressureThreshold)
                toUpdate.Add(artifact);
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a.Owner, null, a);
        }
    }
}
