using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class SpeechBodyFunction : IBodyFunction
    {
        public OrganNode Node => OrganNode.Speech;

        public void Life(IEntity onEntity, OrganState state)
        {
            //TODO: Hook Chat?
        }

        public void OnStateChange(IEntity onEntity, OrganState state)
        {
            //TODO
        }
    }
}
