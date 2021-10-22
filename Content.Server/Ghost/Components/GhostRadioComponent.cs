using System.Collections.Generic;
using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    public class GhostRadioComponent : Component, IRadio
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        public override string Name => "GhostRadio";

        [DataField("channels")]
        private List<int> _channels = new(){1459};

        public IReadOnlyList<int> Channels => _channels;

        public void Receive(string message, int channel, IEntity speaker)
        {
            if (!Owner.TryGetComponent(out ActorComponent? actor))
                return;

            var playerChannel = actor.PlayerSession.ConnectedClient;

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.Radio;
            msg.Message = message;
            //Square brackets are added here to avoid issues with escaping
            msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", speaker.Name));
            _netManager.ServerSendMessage(msg, playerChannel);
        }

        public void Broadcast(string message, IEntity speaker) { }
    }
}
