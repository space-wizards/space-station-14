using Content.Server.StationEvents.Components;
using Content.Server.Nuke;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Nuke;
using Content.Server.Chat.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Server.Popups;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.StationEvents.Events
{
    public sealed class NukeCalibrationRule : StationEventSystem<NukeCalibrationRuleComponent>
    {
        [Dependency] private readonly NukeSystem _nukeSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;

        protected override void Started(EntityUid uid, NukeCalibrationRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            if (!TryGetRandomStation(out var affectedStation))
                return;

            component.AffectedStation = affectedStation.Value;

            var nukeQuery = AllEntityQuery<NukeComponent, TransformComponent>();
            while (nukeQuery.MoveNext(out var nuke, out var nukeComponent, out var nukeTransform))
            {
                // let's not arm the nuke if it isn't on station
                if (CompOrNull<StationMemberComponent>(nukeTransform.GridUid)?.Station != affectedStation)
                    continue;

                if (nukeComponent.Status == NukeStatus.ARMED)
                    continue;

                // If it isn't anchored, then try to anchor it. If we can't anchor it, just continue.
                if (!nukeTransform.Anchored)
                    if (!_transform.AnchorEntity(nuke, nukeTransform))
                        continue;

                _nukeSystem.SetRemainingTime(nuke, component.NukeTimer);
                _nukeSystem.ArmBomb(nuke, nukeComponent);
                component.AffectedNuke = nuke;

                if (!nukeComponent.DiskSlot.HasItem)
                    _popups.PopupEntity(Loc.GetString("station-event-nuke-calibration-arm-popup"), nuke, PopupType.LargeCaution);
                else
                {
                    _transform.SetCoordinates(nukeComponent.DiskSlot.ContainerSlot!.ContainedEntity!.Value, nukeTransform.Coordinates);
                    _popups.PopupEntity(Loc.GetString("station-event-nuke-calibration-arm-and-disk-ejected-popup"), nuke, PopupType.LargeCaution);
                }

                break;
            }
        }

        protected override void Ended(EntityUid uid, NukeCalibrationRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);

            if (!TryComp<NukeComponent>(component.AffectedNuke, out var nukeComp))
                return;

            // Lucky enough, so nuke gets disarmed for you :D
            if (RobustRandom.NextFloat() <= component.AutoDisarmChance)
            {
                // 220
                _nukeSystem.SetRemainingTime(component.AffectedNuke, nukeComp.Timer);
                if (nukeComp.Status != NukeStatus.ARMED)
                    return;

                _nukeSystem.DisarmBomb(component.AffectedNuke, nukeComp);
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-nuke-calibration-disarm-success-announcement"), playSound: false, colorOverride: Color.Green);
                _audioSystem.PlayGlobal(component.AutoDisarmSuccessSound, Filter.Broadcast(), true);

                return;
            }

            if (nukeComp.Status != NukeStatus.ARMED)
                return;

            // Ooops.....
            _nukeSystem.SetDiskBypassEnabled(component.AffectedNuke, true, true, nukeComp);
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-nuke-calibration-disarm-fail-announcement"), playSound: false, colorOverride: Color.Crimson);
            _audioSystem.PlayGlobal(component.AutoDisarmFailedSound, Filter.Broadcast(), true);
        }

        protected override void ActiveTick(EntityUid uid, NukeCalibrationRuleComponent component, GameRuleComponent gameRule, float frameTime)
        {
            base.ActiveTick(uid, component, gameRule, frameTime);

            if (component.FirstAnnouncementMade == true)
                return;

            component.TimeUntilFirstAnnouncement -= frameTime;
            if (component.TimeUntilFirstAnnouncement > 0f)
                return;

            component.FirstAnnouncementMade = true;
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("station-event-nuke-calibration-midway-announcement"), colorOverride: Color.Yellow);
        }
    }
}
