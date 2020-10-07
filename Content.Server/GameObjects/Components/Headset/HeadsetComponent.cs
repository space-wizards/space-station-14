using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Chat;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
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

        private List<int> _channels = new List<int>();

        [ViewVariables(VVAccess.ReadWrite)]
        private int BroadcastFrequency { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int ListenRange { get; private set; }

        public IReadOnlyList<int> Channels => _channels;

        public bool RadioRequested { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // Only listens to speech in exact same position
            serializer.DataField(this, h => h.ListenRange, "listenRange", 0);

            serializer.DataField(ref _channels, "channels", new List<int> {1459});
            serializer.DataField(this, h => h.BroadcastFrequency, "broadcastChannel", 1459);
        }

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
            if (ContainerHelpers.TryGetContainer(Owner, out var container))
            {
                if (!container.Owner.TryGetComponent(out IActorComponent actor))
                    return;

                var playerChannel = actor.playerSession.ConnectedClient;

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                msg.MessageWrap = Loc.GetString("[{0}] {1} says, \"{{0}}\"", channel, source.Name);
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
            message.AddText(Loc.GetString("It is set to broadcast over the {0} frequency.", BroadcastFrequency));

            message.AddText(Loc.GetString("A small screen on the headset displays the following available frequencies:"));
            message.AddText("\n");
            message.AddText(Loc.GetString("Use {0} for the currently tuned frequency.", ";"));
        }
    }
}
