using Content.Shared.Examine;

namespace Content.Server.Headset
{
    public sealed class HeadsetSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HeadsetComponent, ExaminedEvent>(OnExamined);
        }

        private void OnExamined(EntityUid uid, HeadsetComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;
            args.PushMarkup(Loc.GetString("examine-radio-frequency", ("frequency", component.BroadcastFrequency)));
            args.PushMarkup(Loc.GetString("examine-headset"));
            args.PushMarkup(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
        }
    }
}
