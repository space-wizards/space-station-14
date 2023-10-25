using System.Linq;
using Content.Server.Salvage;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Clothing;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

/// <summary>
/// This handles artifacts that are activated by magnets, both salvage and magboots.
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

        if (!EntityQuery<ArtifactMagnetTriggerComponent>().Any())
            return;

        List<EntityUid> toActivate = new();

        //assume that there's more instruments than artifacts
        var query = EntityQueryEnumerator<MagbootsComponent, TransformComponent>();
        while (query.MoveNext(out _, out var magboot, out var magXform))
        {
            if (!magboot.On)
                continue;

            var artiQuery = EntityQueryEnumerator<ArtifactMagnetTriggerComponent, TransformComponent>();
            while (artiQuery.MoveNext(out var artifactUid, out var trigger, out var xform))
            {
                if (!magXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                    continue;

                if (distance > trigger.Range)
                    continue;

                toActivate.Add(artifactUid);
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
