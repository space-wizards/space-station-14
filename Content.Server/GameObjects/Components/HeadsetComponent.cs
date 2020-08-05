using Content.Server.Interfaces;
using Content.Shared.Chat;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NFluidsynth;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class HeadsetComponent : Component, IListen
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public override string Name => "Headset";

        private int _listenRange = 0;

        public int GetListenRange()
        {
            return _listenRange;
        }

        public void HeardSpeech(string speech, IEntity source)
        {
            if (speech.StartsWith(';') && source.TryGetComponent<IActorComponent>(out IActorComponent actor))
            {
                speech = speech.Substring(1);

                var playerChannel = actor.playerSession.ConnectedClient;
                if (playerChannel == null) { return; }

                var msg = _netManager.CreateNetMessage<MsgChatMessage>();

                msg.Channel = ChatChannel.Radio;
                msg.Message = speech;
                msg.MessageWrap = $"{source.Name} says, \"{{0}}\"";
                msg.SenderEntity = source.Uid;
                _netManager.ServerSendMessage(msg, playerChannel);
            }
        }

        public GridCoordinates GetListenerPosition()
        {
            return Owner.Transform.GridPosition;
        }
    }
}
