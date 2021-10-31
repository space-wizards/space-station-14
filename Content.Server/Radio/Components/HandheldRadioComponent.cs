using System.Collections.Generic;
using Content.Server.Chat.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Radio.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
#pragma warning disable 618
    public class HandheldRadioComponent : Component, IUse, IListen, IRadio, IActivate, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        public override string Name => "Radio";

        private RadioSystem _radioSystem = default!;

        private bool _radioOn;
        [DataField("channels")]
        private List<int> _channels = new(){1459};

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        private int BroadcastFrequency { get; set; } = 1459;

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

            RadioOn = false;
        }

        public void Speak(string message)
        {
            _chatManager.EntitySay(Owner, message);
        }

        public bool Use(IEntity user)
        {
            RadioOn = !RadioOn;

            var message = Loc.GetString("handheld-radio-component-on-use",
                                        ("radioState", Loc.GetString(RadioOn ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state")));
            Owner.PopupMessage(user, message);

            return true;
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return Use(eventArgs.User);
        }

        public bool CanListen(string message, IEntity source)
        {
            return RadioOn &&
                   Owner.InRangeUnobstructed(source.Transform.Coordinates, range: ListenRange);
        }

        public void Receive(string message, int channel, IEntity speaker)
        {
            if (RadioOn)
            {
                Speak(message);
            }
        }

        public void Listen(string message, IEntity speaker)
        {
            Broadcast(message, speaker);
        }

        public void Broadcast(string message, IEntity speaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, BroadcastFrequency);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            Use(eventArgs.User);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddText(Loc.GetString("handheld-radio-component-on-examine",("frequency", BroadcastFrequency)));
        }
    }
}
