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
        foreach (var magboot in EntityQuery<MagbootsComponent>())
        {
            if (!magboot.On)
                continue;

            var magXform = Transform(magboot.Owner);

            foreach (var (trigger, xform) in artifactQuery)
            {
                if (!magXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
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

    private void OnMagnetActivated(SalvageMagnetActivatedEvent ev)
    {
        var magXform = Transform(ev.Magnet);

        var toActivate = new List<EntityUid>();
        foreach (var (artifact, xform) in EntityQuery<ArtifactMagnetTriggerComponent, TransformComponent>())
        {
            if (!magXform.Coordinates.TryDistance(EntityManager, xform.Coordinates, out var distance))
                continue;

            if (distance > artifact.Range)
                continue;

            toActivate.Add(artifact.Owner);
        }

        foreach (var a in toActivate)
        {
            _artifact.TryActivateArtifact(a);
        }
    }
}
