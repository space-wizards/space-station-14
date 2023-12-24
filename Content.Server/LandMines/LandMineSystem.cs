using Content.Server.Explosion.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;

namespace Content.Server.LandMines;

public sealed class LandMineSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LandMineComponent, StepOffTriggeredEvent>(HandleStepOffTriggered);
        SubscribeLocalEvent<LandMineComponent, StepTriggerAttemptEvent>(HandleStepTriggerAttempt);
    }

    private static void HandleStepTriggerAttempt(
        EntityUid uid,
        LandMineComponent component,
        ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void HandleStepOffTriggered(EntityUid uid, LandMineComponent component, ref StepOffTriggeredEvent args)
    {
        // This doesn't use TriggerOnStepTrigger since we don't want to display the popup if nothing happens
        // and I didn't feel like making an `AfterTrigger` event
        if (_trigger.Trigger(uid, args.Tripper))
        {
            _popupSystem.PopupCoordinates(
                Loc.GetString("land-mine-triggered", ("mine", uid)),
                Transform(uid).Coordinates,
                args.Tripper,
                PopupType.LargeCaution);
        }
    }
}

