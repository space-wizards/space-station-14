using Content.Server.GameTicking.Rules.Configurations;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.StationEvents.Events
{
    [UsedImplicitly]
    public sealed class FalseAlarm : StationEventSystem
    {
        public override string Prototype => "FalseAlarm";

        public override void Started()
        {
            base.Started();

            var ev = GetRandomEventUnweighted(PrototypeManager, RobustRandom);

            if (ev.Configuration is not StationEventRuleConfiguration cfg)
                return;

            if (cfg.StartAnnouncement != null)
            {
                ChatSystem.DispatchGlobalAnnouncement(Loc.GetString(cfg.StartAnnouncement), playSound: false, colorOverride: Color.Gold);
            }

            if (cfg.StartAudio != null)
            {
                SoundSystem.Play(cfg.StartAudio.GetSound(), Filter.Broadcast(), cfg.StartAudio.Params);
            }
        }
    }
}
