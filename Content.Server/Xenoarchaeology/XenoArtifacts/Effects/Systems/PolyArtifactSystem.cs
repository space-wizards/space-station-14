using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Humanoid;
using Robust.Shared.Random;
using Content.Server.Polymorph.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class PolyArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PolymorphSystem _poly = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PolyArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, PolyArtifactComponent component, ArtifactActivatedEvent args)
    {
        foreach (var target in _lookup.GetEntitiesInRange(uid, component.Range))
        {
            if (HasComp<HumanoidAppearanceComponent>(target))
                _poly.PolymorphEntity(target, component.PolyEntity);
        }
    }
}
