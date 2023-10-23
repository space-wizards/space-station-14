using Content.Server.Polymorph.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class PolyArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary>
    /// On effect trigger polymorphs targets in range.
    /// </summary>
    public override void Initialize()
    {
        SubscribeLocalEvent<PolyArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    /// <summary>
    /// Provided target is alive and is not a zombie, polymorphs the target.
    /// </summary>
    private void OnActivate(EntityUid uid, PolyArtifactComponent component, ArtifactActivatedEvent args)
    {
        var xform = Transform(uid);
        var humanoids = new HashSet<Entity<HumanoidAppearanceComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, component.Range, humanoids);

        foreach (var comp in humanoids)
        {
            var target = comp.Owner;
            if (_mob.IsAlive(target))
            {
                _poly.PolymorphEntity(target, component.PolymorphPrototypeName);
                _audio.PlayPvs(component.PolySound, uid);
            }
        }
    }
}
