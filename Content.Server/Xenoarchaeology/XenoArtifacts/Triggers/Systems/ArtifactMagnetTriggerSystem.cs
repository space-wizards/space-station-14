using Content.Server.Salvage;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

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
