using Content.Shared.Emag.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// Handles emagging medibots and provides api.
/// </summary>
public sealed class MedibotSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmaggableMedibotComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<EmaggableMedibotComponent, GotDeemaggedEvent>(OnDeemagged);
    }

    private void OnEmagged(EntityUid uid, EmaggableMedibotComponent comp, ref GotEmaggedEvent args)
    {
        if (!TryComp<MedibotComponent>(uid, out var medibot))
            return;

        _audio.PlayPredicted(comp.SparkSound, uid, args.UserUid);

        foreach (var (state, treatment) in comp.Replacements)
        {
            comp.Original[state] = medibot.Treatments[state];
            medibot.Treatments[state] = treatment;
        }

        args.Handled = true;
    }

    private void OnDeemagged(Entity<EmaggableMedibotComponent> ent, ref GotDeemaggedEvent args)
    {
        if (!TryComp<MedibotComponent>(ent, out var medibot))
            return;

        foreach (var (state, treatment) in ent.Comp.Original)
        {
            medibot.Treatments[state] = treatment;
        }
    }

    /// <summary>
    /// Get a treatment for a given mob state.
    /// </summary>
    /// <remarks>
    /// This only exists because allowing other execute would allow modifying the dictionary, and Read access does not cover TryGetValue.
    /// </remarks>
    public bool TryGetTreatment(MedibotComponent comp, MobState state, [NotNullWhen(true)] out MedibotTreatment? treatment)
    {
        return comp.Treatments.TryGetValue(state, out treatment);
    }
}
