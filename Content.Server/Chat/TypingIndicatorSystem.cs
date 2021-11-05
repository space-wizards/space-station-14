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
            var entity = EntityManager.GetEntity(ev.EnityId.GetValueOrDefault());
            if(entity.TryGetComponent<TypingIndicatorComponent>(out var typingIndicatorComponent))
            {

            }

            Logger.Info($"User{ev.ClientId} is typing from Entity {entity}!");
        }

        public override void Update(float frameTime)
        {
            //foreach (var comp in EntityManager.EntityQuery<TypingIndicatorComponent>())
            //{
                
            //}

            //base.Update(frameTime);

        }
    }
}
