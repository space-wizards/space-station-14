using System;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// Catches clicks from the client and parses them to relevant entity systems
    /// </summary>
    public class ClickParserSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<ClickEventMessage>();
        }

        /// <summary>
        /// Grab click events sent from the client input system
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            var playerMan = IoCManager.Resolve<IPlayerManager>();
            var session = playerMan.GetSessionByChannel(channel);
            var playerentity = session.AttachedEntity;

            if (playerentity == null)
                return;

            switch (message)
            {
                case ClickEventMessage msg:
                    ParseClickMessage(msg, playerentity);
                    break;
            }
        }

        /// <summary>
        /// Parse click to the relevant entity system
        /// </summary>
        /// <param name="message"></param>
        /// <param name="player"></param>
        private void ParseClickMessage(ClickEventMessage message, IEntity player)
        {
            switch (message.Click)
            {
                case ClickType.Left:
                    EntitySystemManager.GetEntitySystem<InteractionSystem>().UserInteraction(message, player);
                    break;
                case ClickType.Right:
                    //Verb System
                    break;
            }
        }
    }
}
