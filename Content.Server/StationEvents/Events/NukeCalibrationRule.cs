using Content.Server.StationEvents.Components;
using Content.Server.Nuke;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Nuke;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Construction.Components;
using Content.Server.Chat.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Server.Popups;
using Content.Shared.Popups;

namespace Content.Server.StationEvents.Events
{
    public sealed class NukeCalibrationRule : StationEventSystem<NukeCalibrationRuleComponent>
    {
        [Dependency] private readonly NukeSystem _nuke = default!;
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly PopupSystem _popups = default!;

        protected override void Started(EntityUid uid, NukeCalibrationRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            if (!TryGetRandomStation(out var affectedStation))
                return;

            component.AffectedStation = affectedStation.Value;

            var nukeQuery = _entityManager.AllEntityQueryEnumerator<NukeComponent, TransformComponent>();
            while (nukeQuery.MoveNext(out var nuke, out var nukeComponent, out var nukeTransform))
            {
                // let's not arm the nuke if it isn't on station
                if (CompOrNull<StationMemberComponent>(nukeTransform.GridUid)?.Station != affectedStation)
                    continue;

                if (!nukeTransform.Anchored)
                    continue;

                if (nukeComponent.Status == NukeStatus.ARMED)
                    continue;

                _nuke.SetRemainingTime(nuke, component.NukeTimer);
                _nuke.ArmBomb(nuke, nukeComponent);
                component.AffectedNuke = nuke;
                component.AffectedNukeComponent = nukeComponent;
                _popups.PopupEntity(Loc.GetString("station-event-nuke-calibration-arm-popup"), nuke, PopupType.LargeCaution);

                break;
            }
        }

        protected override void Ended(EntityUid uid, NukeCalibrationRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);

            if (component.AffectedNukeComponent == null)
                return;

            // Lucky enough, so nuke gets disarmed for you :D
            if (RobustRandom.NextFloat() <= component.AutoDisarmChance)
            {
                // 220
                _nuke.SetRemainingTime(component.AffectedNuke, component.AffectedNukeComponent.Timer);
                if (component.AffectedNukeComponent.Status != NukeStatus.ARMED)
                    return;

                _nuke.DisarmBomb(component.AffectedNuke, component.AffectedNukeComponent);
                _chat.DispatchGlobalAnnouncement(Loc.GetString("station-event-nuke-calibration-disarm-success-announcement"), playSound: false, colorOverride: Color.Green);
                _audio.PlayGlobal(component.AutoDisarmSuccessSound, Filter.Broadcast(), true);

                return;
            }

            if (component.AffectedNukeComponent.Status != NukeStatus.ARMED)
                return;

            // Ooops.....
            _nuke.SetDiskBypassEnabled(component.AffectedNuke, true, true, component.AffectedNukeComponent);
            _chat.DispatchGlobalAnnouncement(Loc.GetString("station-event-nuke-calibration-disarm-fail-announcement"), playSound: false, colorOverride: Color.Crimson);
            _audio.PlayGlobal(component.AutoDisarmFailedSound, Filter.Broadcast(), true);
        }

        protected override void ActiveTick(EntityUid uid, NukeCalibrationRuleComponent component, GameRuleComponent gameRule, float frameTime)
        {
            base.ActiveTick(uid, component, gameRule, frameTime);

            if (component.AffectedNukeComponent == null)
            {
                ForceEndSelf(uid, gameRule);
                return;
            }

            if (component.FirstAnnouncementMade == true)
                return;

            component.TimeUntilFirstAnnouncement -= frameTime;
            if (component.TimeUntilFirstAnnouncement > 0f)
                return;

            component.FirstAnnouncementMade = true;
            _chat.DispatchGlobalAnnouncement(Loc.GetString("station-event-nuke-calibration-midway-announcement"), colorOverride: Color.Yellow);
        }
    }
}
