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
using SS14.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    public interface IExamine
    {
        /// <summary>
        /// Returns an status examine value for components appended to the end of the description of the entity
        /// </summary>
        /// <returns></returns>
        string Examine();
    }

    public class ExamineSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private IEntityManager _entityManager;
#pragma warning restore 649


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

        private string GetExamineText(IEntity entity)
        {
            //Start a StringBuilder since we have no idea how many times this could be appended to
            var fullExamineText = new StringBuilder();

            //Add an entity description if one is declared
            if (!string.IsNullOrEmpty(entity.Description))
            {
                fullExamineText.Append(entity.Description);
            }

            //Add component statuses from components that report one
            foreach (var examineComponents in entity.GetAllComponents<IExamine>())
            {
                var componentDescription = examineComponents.Examine();
                if (string.IsNullOrWhiteSpace(componentDescription))
                    continue;

                fullExamineText.Append("\n");
                fullExamineText.Append(componentDescription);
            }

            return fullExamineText.ToString();
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            if (message is ExamineSystemMessages.RequestExamineInfoMessage request)
            {
                if (!_entityManager.TryGetEntity(request.EntityUid, out var entity))
                {
                    RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
                        request.EntityUid, "That entity doesn't exist"));
                    return;
                }

                var text = GetExamineText(entity);
                RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(request.EntityUid, text));
            }
        }
    }
}
