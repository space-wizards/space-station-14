using System.Collections.Generic;
using Content.Server.Chat.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Radio.Components
{
    /// <summary>
    ///     Marks an entity as something that can broadcast a radio message over a given frequency.
    /// </summary>
    [RegisterComponent]
    public class RadioBroadcasterComponent : Component
    {
        public override string Name => "RadioBroadcaster";

        public bool Enabled = false;

        [DataField("channels")]
        public List<int> Channels = new(){1459};

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        private int BroadcastFrequency { get; set; } = 1459;

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
