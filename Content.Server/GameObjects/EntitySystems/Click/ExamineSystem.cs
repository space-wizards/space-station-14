#nullable enable
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems.Click
{
    public interface IExamine
    {
        /// <summary>
        /// Returns a status examine value for components appended to the end of the description of the entity
        /// </summary>
        /// <param name="message">The message to append to which will be displayed.</param>
        /// <param name="inDetailsRange">Whether the examiner is within the 'Details' range, allowing you to show information logically only availabe when close to the examined entity.</param>
        void Examine(FormattedMessage message, bool inDetailsRange);
    }

    public class ExamineSystem : ExamineSystemShared
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager = default!;
#pragma warning restore 649

        private static readonly FormattedMessage _entityNotFoundMessage;

        private const float ExamineDetailsRange = 3f;

        static ExamineSystem()
        {
            _entityNotFoundMessage = new FormattedMessage();
            _entityNotFoundMessage.AddText("That entity doesn't exist");
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<ExamineSystemMessages.RequestExamineInfoMessage>(ExamineInfoRequest);

            IoCManager.InjectDependencies(this);
        }

        private static FormattedMessage GetExamineText(IEntity entity, IEntity examiner)
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

            var inDetailsRange = Get<SharedInteractionSystem>()
                .InRangeUnobstructed(examiner.Transform.MapPosition, entity.Transform.MapPosition,
                    ExamineDetailsRange, predicate: entity0 => entity0 == examiner || entity0 == entity, ignoreInsideBlocker: true);

            //Add component statuses from components that report one
            foreach (var examineComponent in entity.GetAllComponents<IExamine>())
            {
                var subMessage = new FormattedMessage();
                examineComponent.Examine(subMessage, inDetailsRange);
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

        private void ExamineInfoRequest(ExamineSystemMessages.RequestExamineInfoMessage request, EntitySessionEventArgs eventArgs)
        {
            var player = (IPlayerSession) eventArgs.SenderSession;
            var session = eventArgs.SenderSession;
            var playerEnt = session.AttachedEntity;
            var channel = player.ConnectedClient;

            DoExamine(playerEnt, channel, request.EntityUid);
        }

        public void DoExamine(IEntity? examiner, INetChannel channel, EntityUid on)
        {
            if (examiner == null
                || !_entityManager.TryGetEntity(on, out var entity)
                || !CanExamine(examiner, entity))
            {
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                    on, _entityNotFoundMessage), channel);
                return;
            }

            var text = GetExamineText(entity, examiner);
            RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(on, text), channel);
        }
    }
}
