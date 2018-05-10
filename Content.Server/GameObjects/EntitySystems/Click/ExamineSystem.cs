using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Text;

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

        public void Examine(ClickEventMessage msg, IEntity player)
        {
            //Get entity clicked upon from UID if valid UID, if not assume no entity clicked upon and null
            IEntity examined = null;
            if (msg.Uid.IsValid())
                examined = EntityManager.GetEntity(msg.Uid);

            if (examined == null)
                return;

            //Verify player has a transform component
            if (!player.TryGetComponent<IServerTransformComponent>(out var playerTransform))
            {
                return;
            }
            //Verify player is on the same map as the entity he clicked on
            else if (msg.Coordinates.MapID != playerTransform.MapID)
            {
                Logger.Warning(string.Format("Player named {0} clicked on a map he isn't located on", player.Name));
                return;
            }

            //Start a stringbuilder since we have no idea how many times this could be appended to
            StringBuilder fullexaminetext = new StringBuilder("This is " + examined.Name);

            //Add an entity description if one is declared
            if (!string.IsNullOrEmpty(examined.Description))
            {
                fullexaminetext.Append(Environment.NewLine + examined.Description);
            }

            //Add component statuses from components that report one
            foreach (var examinecomponents in examined.GetAllComponents<IExamine>())
            {
                string componentdescription = examinecomponents.Examine();
                if (!string.IsNullOrEmpty(componentdescription))
                {
                    fullexaminetext.Append(Environment.NewLine + componentdescription);
                }
            }

            //Send to client chat channel
            //TODO: Fix fact you can only send to all clients because you cant resolve clients from player entities
            IoCManager.Resolve<IChatManager>().DispatchMessage(SS14.Shared.Console.ChatChannel.Visual, fullexaminetext.ToString());
        }
    }
}
