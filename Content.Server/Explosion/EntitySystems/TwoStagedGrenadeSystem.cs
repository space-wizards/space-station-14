using Content.Server.Explosion.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Content.Shared.Audio;
using Content.Server.Audio;
using Content.Server.Singularity.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed class TwoStagedGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TwoStagedGrenadeComponent, TriggerEvent>(OnExplode);
    }

    private void OnExplode(EntityUid uid, TwoStagedGrenadeComponent component, TriggerEvent args)
    {
        if (component.IsSecondStageEnded)
            return;

        component.IsSecondStageBegan = true;
        component.TimeOfExplosion = _timing.CurTime + TimeSpan.FromSeconds(component.ExplosionDelay);
        component.AmbienceStartTime = _timing.CurTime + TimeSpan.FromSeconds(component.AmbienceSoundOffset);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<TwoStagedGrenadeComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (!component.IsSecondStageBegan)
                continue;
            if (!component.IsSecondStageSoundBegan && component.AmbienceStartTime <= _timing.CurTime)
            {
                component.IsSecondStageSoundBegan = true;
                if (TryComp<AmbientSoundComponent>(uid, out var ambientplayer))
                    _ambient.SetAmbience(uid, true, ambientplayer);
            }
            if (!component.IsSecondStageEnded && component.TimeOfExplosion <= _timing.CurTime)
            {
                component.IsSecondStageEnded = true;
                if (!HasComp<ExplodeOnTriggerComponent>(uid) && !HasComp<DeleteOnTriggerComponent>(uid))
                {
                    if (component.ExplodeAfterGravityPull)
                        EnsureComp<ExplodeOnTriggerComponent>(uid);
                    else
                        EnsureComp<DeleteOnTriggerComponent>(uid);
                }
                var sound = EnsureComp<SoundOnTriggerComponent>(uid);
                if (component.EndSound != null)
                    sound.Sound = component.EndSound;
                _triggerSystem.Trigger(uid);
            }
        }
    }
}
