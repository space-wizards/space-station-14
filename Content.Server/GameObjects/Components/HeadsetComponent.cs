using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Chat;
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
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "Headset";

        private int _listenRange = 0;
        private int _channel = 2;
        private RadioSystem _radioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            _radioSystem = _entitySystemManager.GetEntitySystem<RadioSystem>();
        }

        public int GetListenRange()
        {
            return _listenRange;
        }

        public void HeardSpeech(string speech, IEntity source)
        {
            Broadcast(speech);
        }

        public GridCoordinates GetListenerPosition()
        {
            return Owner.Transform.GridPosition;
        }

        public void Receiver(string message)
        {
            if (ContainerHelpers.TryGetContainer(Owner, out IContainer container))
            {
                if (!container.Owner.TryGetComponent<IActorComponent>(out IActorComponent actor)) { return; }

                var playerChannel = actor.playerSession.ConnectedClient;
                if (playerChannel == null) { return; }

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = message;
                msg.MessageWrap = $"From your headset, you hear: \"{{0}}\"";
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public void Broadcast(string message)
        {
            _radioSystem.SpreadMessage(this, message, _channel);
        }

        public int GetChannel()
        {
            return _channel;
        }
    }
}
