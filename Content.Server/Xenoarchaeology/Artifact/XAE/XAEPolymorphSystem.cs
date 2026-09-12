using Content.Server.Polymorph.Systems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that is polymorphing all humanoid entities in range.
/// </summary>
public sealed partial class XAEPolymorphSystem : BaseXAESystem<XAEPolymorphComponent>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private PolymorphSystem _poly = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<Entity<HumanoidProfileComponent>> _humanoids = new();

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEPolymorphComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var duration = ent.Comp.AdditionalDuration;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Duration, out var durationModifier))
            duration = Math.Max(1, durationModifier.Modify(duration));

        _humanoids.Clear();
        _lookup.GetEntitiesInRange(args.Coordinates, ent.Comp.Range, _humanoids);
        foreach (var comp in _humanoids)
        {
            var target = comp.Owner;
            if (!_mob.IsAlive(target))
                continue;

            var uidAfter = _poly.PolymorphEntity(target, ent.Comp.PolymorphPrototypeName);
            if (uidAfter.HasValue)
                _poly.TryAddDuration(uidAfter.Value, (int) duration);

            _audio.PlayPvs(ent.Comp.PolySound, ent);
        }
    }
}
