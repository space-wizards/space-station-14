using Content.Server.Chat;
using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Radio.EntitySystems
{
    public class RadioBroadcasterSystem : EntitySystem
    {
        [Dependency] private readonly RadioSystem _radioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RadioBroadcasterComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<RadioBroadcastOnListenComponent, ChatMessageListenedEvent>(OnChatMessageListened);
        }

        private void OnExamined(EntityUid uid, RadioBroadcasterComponent component, ExaminedEvent args)
        {
            args.PushText(Loc.GetString("handheld-radio-component-on-examine",("frequency", component.BroadcastFrequency)));
        }

        private void OnChatMessageListened(EntityUid uid, RadioBroadcastOnListenComponent component, ChatMessageListenedEvent args)
        {
            _radioSystem.BroadcastRadioMessage(uid, args.Message);
        }
    }
}
