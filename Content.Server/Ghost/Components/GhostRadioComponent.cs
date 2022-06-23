using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    public sealed class GhostRadioComponent : Component, IRadio
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
        private HashSet<string> _channels = new();

        public void Receive(string message, RadioChannelPrototype channel, EntityUid speaker)
        {
            if (!_channels.Contains(channel.ID) || !_entMan.TryGetComponent(Owner, out ActorComponent? actor))
                return;

            var playerChannel = actor.PlayerSession.ConnectedClient;

            var msg = new MsgChatMessage
            {
                Channel = ChatChannel.Radio,
                Message = message,
                //Square brackets are added here to avoid issues with escaping
                MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel.Name}\\]"), ("name", _entMan.GetComponent<MetaDataComponent>(speaker).EntityName))
            };

            _netManager.ServerSendMessage(msg, playerChannel);
        }

        public void Broadcast(string message, EntityUid speaker, RadioChannelPrototype channel) { }
    }
}
