using System.Threading;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using JetBrains.Annotations;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class EvacShuttleBusyRule : StationEventSystem<EvacShuttleBusyRuleComponent>
    {
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        protected override void Started(EntityUid uid, EvacShuttleBusyRuleComponent component,
            GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            // I originally had these set up here but somehow time gets passed as 0 to Loc so IDEK.
            int time;
            string units;
            const string text = "evac-shuttle-busy-starting-announcement";
            var countdownTime = TimeSpan.FromMinutes(99999999); // Obviously placeholder
            const string name = "Centcom";

            if (countdownTime.TotalSeconds < 60)
            {
                time = countdownTime.Seconds;
                units = "eta-units-seconds";
            }
            else
            {
                time = countdownTime.Minutes;
                units = "eta-units-minutes";
            }

            _chatSystem.DispatchGlobalAnnouncement(
                Loc.GetString(
                    text,
                    ("time", time),
                    ("units", Loc.GetString(units))
                ),
                name,
                false,
                null,
                Color.Gold
            );

            _roundEndSystem.ActivateCooldown(countdownTime);
        }

        protected override void Ended(EntityUid uid, EvacShuttleBusyRuleComponent component, GameRuleComponent gameRule,
            GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);

            const string text = "evac-shuttle-busy-ending-announcement";
            const string name = "Centcom";

            _chatSystem.DispatchGlobalAnnouncement(
                Loc.GetString(text),
                name,
                false,
                null,
                Color.Gold
            );
        }

        protected override void ActiveTick(EntityUid uid, EvacShuttleBusyRuleComponent component,
            GameRuleComponent gameRule, float frameTime)
        {
            base.ActiveTick(uid, component, gameRule, frameTime);

            var updates = 0;
            component.FrameTimeAccumulator += frameTime;
            if (component.FrameTimeAccumulator > component.UpdateRate)
            {
                updates = (int) (component.FrameTimeAccumulator / component.UpdateRate);
                component.FrameTimeAccumulator -= component.UpdateRate * updates;
            }

            for (var i = 0; i < updates; i++)
            {
            }
        }
    }
}
