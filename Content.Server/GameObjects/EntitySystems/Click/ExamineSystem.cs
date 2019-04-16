using System;
using System.Text;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;

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
