using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Chat;
using JetBrains.Annotations;
using Robust.Shared.Log;

namespace Content.Server.Speech.EntitySystems
{
    [UsedImplicitly]
    public class TypingIndicatorSystem : SharedTypingIndicatorSystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<ClientTypingMessage>(OnClientTyping);
        }

        private void OnClientTyping(ClientTypingMessage ev)
        {
            Logger.Info($"User{ev} is typing!");
        }
    }
}
