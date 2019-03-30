using System;
using System.Text;
using Content.Shared.Input;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects.Components;
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
        /// <inheritdoc />
        public override void Initialize()
        {
            var inputSys = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSys.BindMap.BindFunction(ContentKeyFunctions.ExamineEntity, new PointerInputCmdHandler(HandleExamine));
        }

        private void HandleExamine(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            if (!(session is IPlayerSession svSession))
                return;

            var playerEnt = svSession.AttachedEntity;
            if (!EntityManager.TryGetEntity(uid, out var examined))
                return;

            //Verify player has a transform component
            if (!playerEnt.TryGetComponent<ITransformComponent>(out var playerTransform))
            {
                return;
            }

            //Verify player is on the same map as the entity he clicked on
            if (coords.MapID != playerTransform.MapID)
            {
                Logger.WarningS("sys.examine", $"Player named {session.Name} clicked on a map he isn't located on");
                return;
            }

            //Start a StringBuilder since we have no idea how many times this could be appended to
            var fullExamineText = new StringBuilder("This is " + examined.Name);

            //Add an entity description if one is declared
            if (!string.IsNullOrEmpty(examined.Description))
            {
                fullExamineText.Append(Environment.NewLine + examined.Description);
            }

            //Add component statuses from components that report one
            foreach (var examineComponents in examined.GetAllComponents<IExamine>())
            {
                var componentDescription = examineComponents.Examine();
                if (string.IsNullOrEmpty(componentDescription))
                    continue;

                fullExamineText.Append(Environment.NewLine);
                fullExamineText.Append(componentDescription);
            }

            //Send to client chat channel
            IoCManager.Resolve<IChatManager>().DispatchMessage(svSession.ConnectedClient, SS14.Shared.Console.ChatChannel.Visual, fullExamineText.ToString(), session.SessionId);
        }
    }
}
