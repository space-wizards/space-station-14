using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Chat;
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
            //in here we can do some server side checks before the client system finally renders the indicators

            RaiseNetworkEvent(ev);

        }

    }
}
