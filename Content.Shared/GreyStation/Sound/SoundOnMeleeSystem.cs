using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.GreyStation.Sound;

public sealed class SoundOnMeleeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoundOnMeleeComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<SoundOnMeleeComponent> ent, ref MeleeHitEvent args)
    {
        // no sound if you swing at the air
        if (args.HitEntities.Count == 0)
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextSound)
            return;

        ent.Comp.NextSound = now + ent.Comp.Cooldown;
        _audio.PlayPredicted(ent.Comp.Sound, ent, ent);
    }
}
