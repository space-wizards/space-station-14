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
        SubscribeLocalEvent<LandMineComponent, StepTriggeredOnEvent>(HandleStepOnTriggered);
        SubscribeLocalEvent<LandMineComponent, StepTriggeredOffEvent>(HandleStepOffTriggered);

        SubscribeLocalEvent<LandMineComponent, StepTriggerAttemptEvent>(HandleStepTriggerAttempt);
    }

    private void HandleStepOnTriggered(EntityUid uid, LandMineComponent component, ref StepTriggeredOnEvent args)
    {
        _popupSystem.PopupCoordinates(
            Loc.GetString("land-mine-triggered", ("mine", uid)),
            Transform(uid).Coordinates,
            args.Tripper,
            PopupType.LargeCaution);

        _audioSystem.PlayPvs(component.Sound, uid);
    }

    private void HandleStepOffTriggered(EntityUid uid, LandMineComponent component, ref StepTriggeredOffEvent args)
    {
        _trigger.Trigger(uid, args.Tripper);
    }

    private static void HandleStepTriggerAttempt(EntityUid uid, LandMineComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }
}
