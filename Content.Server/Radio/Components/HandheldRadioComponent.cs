using System.Collections.Generic;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server.Radio.Components
{
    [RegisterComponent]
    [ComponentProtoName("Radio")]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    [ComponentReference(typeof(IActivate))]
#pragma warning disable 618
    public sealed class HandheldRadioComponent : Component, IListen, IRadio, IActivate
#pragma warning restore 618
    {
        private ChatSystem _chatSystem = default!;
        private RadioSystem _radioSystem = default!;

        private bool _radioOn;
        [DataField("channels")]
        private List<int> _channels = new(){1459};

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        public int BroadcastFrequency { get; set; } = 1459;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("listenRange")] public int ListenRange { get; private set; } = 7;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool RadioOn
        {
            get => _radioOn;
            private set
            {
                _radioOn = value;
                Dirty();
            }
        }

        [ViewVariables] public IReadOnlyList<int> Channels => _channels;

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
            _chatSystem = EntitySystem.Get<ChatSystem>();

            RadioOn = false;
        }

        public void Speak(string message)
        {
            _chatSystem.TrySendInGameICMessage(Owner, message, InGameICChatType.Speak, false);
        }

        public bool Use(EntityUid user)
        {
            RadioOn = !RadioOn;

            var message = Loc.GetString("handheld-radio-component-on-use",
                                        ("radioState", Loc.GetString(RadioOn ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state")));
            Owner.PopupMessage(user, message);

            return true;
        }

        public bool CanListen(string message, EntityUid source)
        {
            return RadioOn &&
                   EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(Owner, source, range: ListenRange);
        }

        public void Receive(string message, int channel, EntityUid speaker)
        {
            if (RadioOn)
            {
                Speak(message);
            }
        }

        public void Listen(string message, EntityUid speaker)
        {
            Broadcast(message, speaker);
        }

        public void Broadcast(string message, EntityUid speaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, BroadcastFrequency);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            Use(eventArgs.User);
        }
    }
}
