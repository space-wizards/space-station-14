using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Chat;
using Content.Shared.GameObjects.Components.Items;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NFluidsynth;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    public class HeadsetComponent : Component, IListen, IRadio
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "Headset";

        //only listens to speech in exact same position
        private int _listenRange = 0;
        private List<int> _channels = new List<int>();
        private int _broadcastChannel;
        private RadioSystem _radioSystem = default!;
        public bool RadioRequested = false;

        public override void Initialize()
        {
            base.Initialize();

            _radioSystem = _entitySystemManager.GetEntitySystem<RadioSystem>();
            _channels.Add(1457);
            _broadcastChannel = 1457;
        }

        public int GetListenRange()
        {
            return _listenRange;
        }

        public void HeardSpeech(string speech, IEntity source)
        {
            if (RadioRequested)
            {
                Broadcast(speech, source);
            }
            RadioRequested = false;
        }

        public GridCoordinates GetListenerPosition()
        {
            return Owner.Transform.GridPosition;
        }

        public void Receiver(string message, int channel, IEntity source)
        {
            if (ContainerHelpers.TryGetContainer(Owner, out IContainer container))
            {
                if (!container.Owner.TryGetComponent<IActorComponent>(out IActorComponent actor)) { return; }

                var playerChannel = actor.playerSession.ConnectedClient;
                if (playerChannel == null) { return; }

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                msg.MessageWrap = $"[{channel.ToString()}] {source.Name} says, \"{{0}}\"";
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public void Broadcast(string message, IEntity speaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, _broadcastChannel);
        }

        public List<int> GetChannels()
        {
            return _channels;
        }
    }
}
