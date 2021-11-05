using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Chat;
using Robust.Shared.GameObjects;

namespace Content.Client.Chat
{
    public class TypingIndicatorSystem : SharedTypingIndicatorSystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<ClientChatInputActiveMessage>(HandleClientTyping);
            base.Initialize();
        }


        private void HandleClientTyping(ClientChatInputActiveMessage msg)
        {
            RaiseNetworkEvent(new ClientTypingMessage());
        }

        [Serializable]
        public sealed class ClientChatInputActiveMessage : EntityEventArgs
        {
            

        }
    }
}
