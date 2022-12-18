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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var artifactQuery = EntityQuery<ArtifactMusicTriggerComponent, TransformComponent>().ToArray();
        if (!artifactQuery.Any())
            return;

        List<EntityUid> toActivate = new();

        //assume that there's more instruments than artifacts
        foreach (var activeinstrument in EntityQuery<ActiveInstrumentComponent>())
        {
            var instXform = Transform(activeinstrument.Owner);

            foreach (var (trigger, xform) in artifactQuery)
            {
                if (!instXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                    continue;

                if (distance > trigger.Range)
                    continue;

                toActivate.Add(trigger.Owner);
            }
        }

        foreach (var a in toActivate)
        {
            _artifact.TryActivateArtifact(a);
        }
    }
}
