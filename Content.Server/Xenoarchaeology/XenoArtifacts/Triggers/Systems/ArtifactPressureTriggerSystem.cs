using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

/// <summary>
/// This handles activation upon certain pressure thresholds.
/// </summary>
[InjectDependencies]
public sealed partial class ArtifactPressureTriggerSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private ArtifactSystem _artifactSystem = default!;
    [Dependency] private TransformSystem _transformSystem = default!;

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
