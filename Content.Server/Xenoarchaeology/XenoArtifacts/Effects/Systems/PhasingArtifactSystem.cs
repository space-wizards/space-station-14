using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
///     Handles allowing activated artifacts to phase through walls.
/// </summary>
public sealed class PhasingArtifactSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhasingArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, PhasingArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            _physics.SetHard(uid, fixture, false, fixtures);
        }
    }
}
