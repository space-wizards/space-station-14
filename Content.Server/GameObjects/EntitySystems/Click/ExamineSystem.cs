using System;
using System.Text;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Players;
using SS14.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems
{
    public interface IExamine
    {
        /// <summary>
        /// Returns an status examine value for components appended to the end of the description of the entity
        /// </summary>
        void Examine(FormattedMessage message);
    }

    public class ExamineSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private IEntityManager _entityManager;
#pragma warning restore 649

        private static readonly FormattedMessage _entityNotFoundMessage;

        static ExamineSystem()
        {
            _entityNotFoundMessage = new FormattedMessage();
            _entityNotFoundMessage.AddText("That entity doesn't exist");
        }

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<ExamineSystemMessages.RequestExamineInfoMessage>();
        }

        private FormattedMessage GetExamineText(IEntity entity)
        {
            var message = new FormattedMessage();

            var doNewline = false;

            //Add an entity description if one is declared
            if (!string.IsNullOrEmpty(entity.Description))
            {
                message.AddText(entity.Description);
                doNewline = true;
            }

            message.PushColor(Color.DarkGray);

            var subMessage = new FormattedMessage();
            //Add component statuses from components that report one
            foreach (var examineComponents in entity.GetAllComponents<IExamine>())
            {
                examineComponents.Examine(subMessage);
                if (subMessage.Tags.Count == 0)
                    continue;

                if (doNewline)
                {
                    message.AddText("\n");
                    doNewline = false;
                }
                message.AddMessage(subMessage);
            }

            message.Pop();

            return message;
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            if (message is ExamineSystemMessages.RequestExamineInfoMessage request)
            {
                if (!_entityManager.TryGetEntity(request.EntityUid, out var entity))
                {
                    RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                        request.EntityUid, _entityNotFoundMessage));
                    return;
                }

                var text = GetExamineText(entity);
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(request.EntityUid, text));
            }
        }
    }
}
