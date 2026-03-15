using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.BloodCult;
using Robust.Shared.Audio.Systems;

namespace Content.Client.BloodCult;

/// <summary>
/// Plays cast sound with prediction when spell actions are triggered so the performer gets immediate feedback.
/// Spell logic remains in server CultistSpellSystem.
/// </summary>
public sealed class CultistSpellPredictionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, EventCultistStudyVeil>(OnSpellAction);
        SubscribeLocalEvent<BloodCultistComponent, EventCultistSummonDagger>(OnSpellAction);
        SubscribeLocalEvent<BloodCultistComponent, EventCultistSanguineDream>(OnSpellAction);
        SubscribeLocalEvent<BloodCultistComponent, EventCultistTwistedConstruction>(OnSpellAction);
    }

    private void OnSpellAction<T>(Entity<BloodCultistComponent> ent, ref T args) where T : BaseActionEvent
    {
        PlayCastSoundIfPresent(args.Performer, args.Action);
    }

    private void PlayCastSoundIfPresent(EntityUid performer, Entity<ActionComponent> action)
    {
        if (!TryComp(action.Owner, out CultistSpellComponent? spellComp) || spellComp.CastSound == null)
            return;

        _audio.PlayPredicted(spellComp.CastSound, performer, performer);
    }
}
