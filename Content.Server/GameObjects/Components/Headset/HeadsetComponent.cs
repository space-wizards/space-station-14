using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Chat;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Headset
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    public class HeadsetComponent : Component, IListen, IRadio, IExamine
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public override string Name => "Headset";

        private RadioSystem _radioSystem = default!;

        [DataField("channels")]
        private List<int> _channels = new(){1459};

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        private int BroadcastFrequency { get; set; } = 1459;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; }

        public IReadOnlyList<int> Channels => _channels;

        public bool RadioRequested { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
        }

        public bool CanListen(string message, IEntity source)
        {
            return RadioRequested;
        }

        public void Receive(string message, int channel, IEntity source)
        {
            if (Owner.TryGetContainer(out var container))
            {
                if (!container.Owner.TryGetComponent(out IActorComponent? actor))
                    return;

                var playerChannel = actor.playerSession.ConnectedClient;

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                //Square brackets are added here to avoid issues with escaping
                msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"[{channel}]"), ("name", source.Name));
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public void Listen(string message, IEntity speaker)
        {
            Broadcast(message, speaker);
        }

        public void Broadcast(string message, IEntity speaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, BroadcastFrequency);
            RadioRequested = false;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddText(Loc.GetString("examine-radio-frequency", ("frequency", BroadcastFrequency)));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset"));
            message.AddText("\n");
            message.AddText(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
        }
    }
}
