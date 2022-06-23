using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Headset
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
#pragma warning disable 618
    public sealed class HeadsetComponent : Component, IListen, IRadio
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;

        private RadioSystem _radioSystem = default!;

        [DataField("channels", customTypeSerializer:typeof(PrototypeIdListSerializer<RadioChannelPrototype>))]
        private List<String> _channels = new(){"broadcast"};

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        public int BroadcastFrequency { get; set; } = 0;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; }

        public IReadOnlyList<RadioChannelPrototype> Channels => getChannels();

        public bool RadioRequested { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            _radioSystem = EntitySystem.Get<RadioSystem>();
        }

        private List<RadioChannelPrototype> getChannels()
        {
            List<RadioChannelPrototype> ret = new List<RadioChannelPrototype>();
            foreach (string channelId in _channels)
            {
                if (IoCManager.Resolve<IPrototypeManager>().TryIndex(channelId, out RadioChannelPrototype? proto))
                    ret.Add(proto);
            }
            return ret;
        }

        public bool CanListen(string message, EntityUid source)
        {
            return RadioRequested;
        }

        public void Receive(string message, RadioChannelPrototype channel, EntityUid source)
        {
            if (Owner.TryGetContainer(out var container))
            {
                if (!_entMan.TryGetComponent(container.Owner, out ActorComponent? actor))
                    return;

                var playerChannel = actor.PlayerSession.ConnectedClient;

                var msg = new MsgChatMessage();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                //Square brackets are added here to avoid issues with escaping
                msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", _entMan.GetComponent<MetaDataComponent>(source).EntityName));
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public void Listen(string message, EntityUid speaker, RadioChannelPrototype channel)
        {
            Broadcast(message, speaker, channel);
        }

        public void Broadcast(string message, EntityUid speaker, RadioChannelPrototype channel)
        {
            if (getChannels().Contains(channel))
                _radioSystem.SpreadMessage(this, speaker, message, channel);
            RadioRequested = false;
        }
    }
}
