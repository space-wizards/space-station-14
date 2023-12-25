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

        List<Entity<ArtifactComponent>> toUpdate = new();
        var query = EntityQueryEnumerator<ArtifactPressureTriggerComponent, ArtifactComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var artifact, out var transform))
        {
            var environment = _atmosphereSystem.GetTileMixture((uid, transform));
            if (environment == null)
                continue;

            var pressure = environment.Pressure;
            if (pressure >= trigger.MaxPressureThreshold || pressure <= trigger.MinPressureThreshold)
                toUpdate.Add((uid, artifact));
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a, null, a);
        }
    }
}
