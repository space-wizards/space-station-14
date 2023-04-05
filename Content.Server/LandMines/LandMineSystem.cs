using Content.Server.Explosion.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Player;

namespace Content.Server.LandMines;

public sealed class LandMineSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<LandMineComponent, StepTriggeredEvent>(HandleTriggered);
        SubscribeLocalEvent<LandMineComponent, StepTriggerAttemptEvent>(HandleTriggerAttempt);
    }

    private static void HandleTriggerAttempt(
        EntityUid uid,
        LandMineComponent component,
        ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void HandleTriggered(EntityUid uid, LandMineComponent component, ref StepTriggeredEvent args)
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

