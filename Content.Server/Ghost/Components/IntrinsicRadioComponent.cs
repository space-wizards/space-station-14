using Content.Server.Radio.Components;
using Content.Server.VoiceMask;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Components
{
    /// <summary>
    /// Add to a particular entity to let it receive messages from the specified channels.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    public sealed class IntrinsicRadioComponent : Component, IRadio
    {
        // TODO: This class is yuck
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>), required: true)]
        private HashSet<string> _channels = new();

        public void Receive(string message, RadioChannelPrototype channel, EntityUid speaker)
        {
            if (!_channels.Contains(channel.ID) || !_entMan.TryGetComponent(Owner, out ActorComponent? actor))
                return;

            var playerChannel = actor.PlayerSession.ConnectedClient;

            var name = _entMan.GetComponent<MetaDataComponent>(speaker).EntityName;

            if (_entMan.TryGetComponent(speaker, out VoiceMaskComponent? mask) && mask.Enabled)
            {
                name = Identity.Name(speaker, _entMan);
            }

            message = FormattedMessage.EscapeText(message);
            name = FormattedMessage.EscapeText(name);

            var msg = new MsgChatMessage
            {
                Channel = ChatChannel.Radio,
                Message = message,
                WrappedMessage = Loc.GetString("chat-radio-message-wrap", ("color", channel.Color), ("channel", $"\\[{channel.LocalizedName}\\]"), ("name", name), ("message", message))
            };

            _netManager.ServerSendMessage(msg, playerChannel);
        }

        public void Broadcast(string message, EntityUid speaker, RadioChannelPrototype channel) { }
    }
}
