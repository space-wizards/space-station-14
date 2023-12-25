using Content.Server.Explosion.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.LandMines;

public sealed class LandMineSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
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
        PlayLandmineActivatedSound(uid, component);

        if (!component.ExplodeImmediately)
        {
            return;
        }

        TriggerLandmine(uid, args.Tripper);
    }

    private void PlayLandmineActivatedSound(EntityUid uid, LandMineComponent component)
    {
        var xform = _entityManager.GetComponent<TransformComponent>(uid);
        var landmineMapCoords = _transformSystem.GetMapCoordinates(xform);

        var filter = Filter.Pvs(landmineMapCoords).AddInRange(landmineMapCoords, component.TriggerAudioRange);
        var landmineEntityCoords = EntityCoordinates.FromMap(_mapManager, landmineMapCoords);

        // I wonder, where do I get a beep sound?
        // _audioSystem.PlayStatic(sound, filter, landmineEntityCoords, true, sound.Params);
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
