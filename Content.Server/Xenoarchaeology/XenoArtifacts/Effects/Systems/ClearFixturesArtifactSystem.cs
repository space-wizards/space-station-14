using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
///     Handles allowing activated artifacts to phase through walls.
/// </summary>
public sealed class ClearFixturesArtifactSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClearFixturesArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, ClearFixturesArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        foreach (var (_, fixture) in fixtures.Fixtures)
        {
            if (!fixture.Hard)
                continue;

            fixture.CollisionLayer = (int) CollisionGroup.None;
            fixture.CollisionMask = (int) CollisionGroup.None;
        }
    }
}
