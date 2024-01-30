using Content.Server.Explosion.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.LandMines;

public sealed class LandMineSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LandMineComponent, StepTriggeredEvent>(HandleStepTriggered);

        SubscribeLocalEvent<LandMineComponent, StepTriggerAttemptEvent>(HandleStepTriggerAttempt);
    }

    private void HandleStepTriggered(EntityUid uid, LandMineComponent component, ref StepTriggeredEvent args)
    {
        if (component.TriggerImmediately && !args.IsStepOff ||
            !component.TriggerImmediately && args.IsStepOff)
        {
            _trigger.Trigger(uid, args.Tripper);
        }
        else if (!component.TriggerImmediately && !args.IsStepOff)
        {
            ShowLandminePopup(uid, args.Tripper);
            PlayLandmineActivatedSound(uid, component);
        }
    }

    private void ShowLandminePopup(EntityUid uid, EntityUid tripper)
    {
        _popupSystem.PopupCoordinates(
                Loc.GetString("land-mine-triggered", ("mine", uid)),
                Transform(uid).Coordinates,
                tripper,
                PopupType.LargeCaution);
    }

    private void PlayLandmineActivatedSound(EntityUid uid, LandMineComponent component)
    {
        SoundSpecifier? triggerSound = component.TriggerSound;

        if (triggerSound == null)
        {
            return;
        }

        _audioSystem.PlayPvs(triggerSound, uid);
    }

    private static void HandleStepTriggerAttempt(EntityUid uid, LandMineComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }
}
