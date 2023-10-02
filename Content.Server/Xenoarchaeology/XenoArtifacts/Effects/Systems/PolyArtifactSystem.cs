using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Humanoid;
using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class PolyArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<PolyArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    /// <summary>
    /// Provided target is alive and is not a zombie, polymorphs the target.
    /// </summary>
    private void OnActivate(EntityUid uid, PolyArtifactComponent component, ArtifactActivatedEvent args)
    {
        foreach (var target in _lookup.GetEntitiesInRange(uid, component.Range))
        {
            if (_mob.IsDead(target) || _mob.IsCritical(target))
                return;


            else if (HasComp<HumanoidAppearanceComponent>(target) && !HasComp<ZombieComponent>(target))
            {
                _poly.PolymorphEntity(target, "ArtifactMonkey");
                _audio.PlayPvs(component.PolySound, uid);
            }
        }
    }
}
