using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Radio
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    public class HandheldRadioComponent : Component, IUse, IListen, IRadio, IActivate, IExamine
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        public override string Name => "Radio";

        private RadioSystem _radioSystem = default!;

        private bool _radioOn;
        private List<int> _channels = new();

        [ViewVariables(VVAccess.ReadWrite)]
        private int BroadcastFrequency { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int ListenRange { get; private set; }

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, h => h.ListenRange, "listenRange", 7);
            serializer.DataField(ref _channels, "channels", new List<int> {1459});
            serializer.DataField(this, h => h.BroadcastFrequency, "broadcastChannel", 1459);
        }

        public override void Initialize()
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

            var message = Loc.GetString($"The radio is now {(RadioOn ? "on" : "off")}.");
            Owner.PopupMessage(user, message);

            return true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return Use(eventArgs.User);
        }

        public bool CanListen(string message, IEntity source)
        {
            return RadioOn &&
                   Owner.Transform.Coordinates.TryDistance(Owner.EntityManager, source.Transform.Coordinates, out var distance) &&
                   distance <= ListenRange;
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

        public void Activate(ActivateEventArgs eventArgs)
        {
            Use(eventArgs.User);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddText(Loc.GetString("It is set to broadcast over the {0} frequency.", BroadcastFrequency));
        }
    }
}
