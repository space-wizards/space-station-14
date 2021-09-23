using System.Collections.Generic;
using Content.Server.Chat.Managers;
using Content.Server.Radio.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public class RadioListenerSystem : EntitySystem
    {
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RadioReceiverComponent, ExaminedEvent>(OnListenerExamined);
            SubscribeLocalEvent<
        }

        private void OnListenerExamined(EntityUid uid, RadioReceiverComponent component, ExaminedEvent args)
        {
            args.PushText(Loc.GetString("examine-radio-frequency", ("frequency", component.BroadcastFrequency)));
            args.PushText(Loc.GetString("examine-headset"));
            args.PushText(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
        }

        private void ToggleListener(EntityUid uid, RadioReceiverComponent component)
        {
            component.Enabled = !component.Enabled;

            if (component.Enabled)
            {
                foreach (var ch in component.Channels)
                {
                    _radioSystem.AddListener(uid, ch);
                }
            }
            else
            {
                foreach (var ch in component.Channels)
                {
                    _radioSystem.RemoveListener(uid, ch);
                }
            }
        }

        public void Listen(string message, IEntity speaker)
        {
            Broadcast(message, speaker);
        }
    }
}
