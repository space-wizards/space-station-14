using Content.Shared.Emag.Systems;
using Robust.Shared.Audio;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// Handles emagging medibots
/// </summary>
public sealed class MedibotSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmaggableMedibotComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(EntityUid uid, EmaggableMedibotComponent comp, ref GotEmaggedEvent args)
    {
        if (!TryComp<MedibotComponent>(uid, out var medibot))
            return;

        _audio.PlayPredicted(comp.SparkSound, uid, args.UserUid, AudioParams.Default.WithVolume(8));

        medibot.StandardMed = comp.StandardMed;
        medibot.StandardMedAmount = comp.StandardMedAmount;
        medibot.EmergencyMed = comp.EmergencyMed;
        medibot.EmergencyMedAmount = comp.EmergencyMedAmount;
        args.Handled = true;
    }
}
