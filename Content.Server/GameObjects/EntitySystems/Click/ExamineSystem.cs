using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
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

    public class ExamineSystem : ExamineSystemShared
    {
#pragma warning disable 649
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IPlayerManager _playerManager;
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

            //Add component statuses from components that report one
            foreach (var examineComponents in entity.GetAllComponents<IExamine>())
            {
                var subMessage = new FormattedMessage();
                examineComponents.Examine(subMessage);
                if (subMessage.Tags.Count == 0)
                    continue;

                if (doNewline)
                    message.AddText("\n");

                message.AddMessage(subMessage);
                doNewline = true;
            }

            message.Pop();

            return message;
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            if (!(message is ExamineSystemMessages.RequestExamineInfoMessage request))
                return;

            var session = _playerManager.GetSessionByChannel(channel);
            var playerEnt = session.AttachedEntity;

            if (playerEnt == null
                || !_entityManager.TryGetEntity(request.EntityUid, out var entity)
                || !CanExamine(playerEnt, entity))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    request.EntityUid, _entityNotFoundMessage), channel);
                return;
            }

            var text = GetExamineText(entity);
            RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(request.EntityUid, text), channel);
        }
    }
}
