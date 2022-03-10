using Content.Server.Radio.Components;
using Content.Server.Administration.Managers;
using Content.Shared.Chat;
using Robust.Server.GameObjects;
using Robust.Shared.Network;

namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    public sealed class GhostRadioComponent : Component, IRadio
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [Dependency] private readonly IAdminManager _adminManager = default!;


        [DataField("channels")]
        private List<int> _channels = new(){1459};

        public IReadOnlyList<int> Channels => _channels;

        public void Receive(string message, int channel, string speakerVoice, string adminVoice)
        {
            if (!_entMan.TryGetComponent(Owner, out ActorComponent? actor))
                return;

            var playerChannel = actor.PlayerSession.ConnectedClient;

            var msg = _netManager.CreateNetMessage<MsgChatMessage>();

            msg.Channel = ChatChannel.Radio;
            msg.Message = message;
            //Square brackets are added here to avoid issues with escaping
            if (!_adminManager.IsAdmin(actor.PlayerSession))
            {
                msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", speakerVoice));
            } else
            {
                msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", adminVoice));
            }
            _netManager.ServerSendMessage(msg, playerChannel);
        }

        public void Broadcast(string message, EntityUid speaker) { }
    }
}
