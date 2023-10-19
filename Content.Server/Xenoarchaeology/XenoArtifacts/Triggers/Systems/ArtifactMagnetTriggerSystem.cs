using System.Linq;
using Content.Server.Salvage;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Clothing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class ArtifactMagnetTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SalvageMagnetActivatedEvent>(OnMagnetActivated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var artifactQuery = EntityQuery<ArtifactMagnetTriggerComponent, TransformComponent>().ToHashSet();
        if (!artifactQuery.Any())
            return;

        List<EntityUid> toActivate = new();

        //assume that there's more instruments than artifacts
        var query = EntityQueryEnumerator<MagbootsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var magboot, out var magXform))
        {
            if (!magboot.On)
                continue;

            foreach (var (trigger, xform) in artifactQuery)
            {
                if (!magXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
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

    private void OnMagnetActivated(SalvageMagnetActivatedEvent ev)
    {
        var magXform = Transform(ev.Magnet);

        var toActivate = new List<EntityUid>();
        var query = EntityQueryEnumerator<ArtifactMagnetTriggerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var artifact, out var xform))
        {
            if (!magXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                continue;

            if (distance > artifact.Range)
                continue;

            toActivate.Add(uid);
        }

        foreach (var a in toActivate)
        {
            _artifact.TryActivateArtifact(a);
        }
    }
}
