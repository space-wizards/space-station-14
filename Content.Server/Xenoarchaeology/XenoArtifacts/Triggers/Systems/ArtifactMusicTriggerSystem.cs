using System.Linq;
using Content.Server.Instruments;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

/// <summary>
/// This handles activating an artifact when music is playing nearby
/// </summary>
public sealed class ArtifactMusicTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    private readonly List<Entity<ArtifactMusicTriggerComponent, TransformComponent>> _artifacts = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _artifacts.Clear();
        var artifactQuery = EntityQueryEnumerator<ArtifactMusicTriggerComponent, TransformComponent>();
        while (artifactQuery.MoveNext(out var uid, out var trigger, out var xform))
        {
            _artifacts.Add((uid, trigger, xform));
        }

        if (!_artifacts.Any())
            return;

        List<EntityUid> toActivate = new();
        var query = EntityQueryEnumerator<ActiveInstrumentComponent, TransformComponent>();

        //assume that there's more instruments than artifacts
        while (query.MoveNext(out _, out var instXform))
        {
            foreach (var (uid, trigger, xform) in _artifacts)
            {
                if (!instXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                    continue;

                if (distance > trigger.Range)
                    continue;

                toActivate.Add(uid);
            }
        }

        foreach (var a in toActivate)
        {
            _artifact.TryActivateArtifact(a);
        }
    }
}
