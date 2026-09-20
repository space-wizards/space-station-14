using Content.Shared.Audio;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.CosmicCult;

public sealed partial class CosmicColossusSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;

    [SubscribeLocalEvent]
    private void OnColossusStunEnded(Entity<CosmicColossusComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key == SharedStunSystem.StunId && _mobState.IsAlive(ent))
            _appearance.SetData(ent, ColossusVisuals.Visuals, ColossusStatus.Dead);
    }

    [SubscribeLocalEvent]
    private void OnColossusMobState(Entity<CosmicColossusComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            _appearance.SetData(ent, ColossusVisuals.Visuals, ColossusStatus.Dead);
            _audio.PlayPredicted(ent.Comp.DeathSfx, ent, ent);
            _ambient.SetAmbience(ent, false);

            ent.Comp.HibernationTimer = null;
        }
    }
}
