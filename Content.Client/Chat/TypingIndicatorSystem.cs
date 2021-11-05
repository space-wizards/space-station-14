using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Chat;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Chat
{
    public class TypingIndicatorSystem : SharedTypingIndicatorSystem
    {

        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ClientChatInputActiveMessage>(HandleClientTyping);
            base.Initialize();
        }


        private void HandleClientTyping(ClientChatInputActiveMessage msg)
        {
            var player = _playerManager.LocalPlayer;
            if (player == null) return;
            RaiseNetworkEvent(new ClientTypingMessage(player.UserId, player.ControlledEntity?.Uid));
        }

        [Serializable]
        public sealed class ClientChatInputActiveMessage : EntityEventArgs
        {
            

        }
    }
}
