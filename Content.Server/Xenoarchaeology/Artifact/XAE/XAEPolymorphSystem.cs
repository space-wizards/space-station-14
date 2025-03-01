using Content.Server.Polymorph.Systems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAEPolymorphSystem : BaseXAESystem<XAEPolymorphComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEPolymorphComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var humanoids = new HashSet<Entity<HumanoidAppearanceComponent>>();
        _lookup.GetEntitiesInRange(args.Coordinates, ent.Comp.Range, humanoids);

        foreach (var comp in humanoids)
        {
            var target = comp.Owner;
            if (!_mob.IsAlive(target))
                continue;

            _poly.PolymorphEntity(target, ent.Comp.PolymorphPrototypeName);
            _audio.PlayPvs(ent.Comp.PolySound, ent);
        }
    }
}
