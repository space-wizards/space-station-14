using System.Collections.Generic;
using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Robust.Server.GameObjects;
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
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "GhostRadio";

        [DataField("channels")]
        private List<int> _channels = new(){1459};

        public IReadOnlyList<int> Channels => _channels;

        public void Receive(string message, int channel, EntityUid speaker)
        {
            if (!_entMan.TryGetComponent(Owner, out ActorComponent? actor))
                return;

            var playerChannel = actor.PlayerSession.ConnectedClient;

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.Radio;
            msg.Message = message;
            //Square brackets are added here to avoid issues with escaping
            msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", Name: _entMan.GetComponent<MetaDataComponent>(speaker).EntityName));
            _netManager.ServerSendMessage(msg, playerChannel);
        }

        public void Broadcast(string message, EntityUid speaker) { }
    }
}
