using Content.Shared.Chat;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

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
            RaiseNetworkEvent(new RemoteClientTypingMessage(ev.ClientId, ev.Owner));
        }
    }
}
