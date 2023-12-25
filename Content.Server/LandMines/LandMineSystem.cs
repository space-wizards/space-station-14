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
        SubscribeLocalEvent<LandMineComponent, StepTriggeredEvent>(HandleStepTriggered);

        SubscribeLocalEvent<LandMineComponent, StepTriggerAttemptEvent>(HandleStepTriggerAttempt);
    }

    private void HandleStepOffTriggered(EntityUid uid, LandMineComponent component, ref StepOffTriggeredEvent args)
    {
        if (component.ExplodeImmediately)
        {
            return;
        }

        TriggerLandmine(uid, args.Tripper);
    }

    private void HandleStepTriggered(EntityUid uid, LandMineComponent component, ref StepTriggeredEvent args)
    {
        if (!component.ExplodeImmediately)
        {
            return;
        }

        TriggerLandmine(uid, args.Tripper);
    }

    private void TriggerLandmine(EntityUid uid, EntityUid tripper)
    {
        if (_trigger.Trigger(uid, tripper))
        {
            _popupSystem.PopupCoordinates(
                Loc.GetString("land-mine-triggered", ("mine", uid)),
                Transform(uid).Coordinates,
                tripper,
                PopupType.LargeCaution);
        }
    }

    private static void HandleStepTriggerAttempt(EntityUid uid, LandMineComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }
}
