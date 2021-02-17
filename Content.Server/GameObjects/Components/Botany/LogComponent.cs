using System.Threading.Tasks;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Botany
{
    [RegisterComponent]
    public class LogComponent : Component, IInteractUsing
    {
        public override string Name => "Log";

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
                return false;

            if (eventArgs.Using.HasComponent<BotanySharpComponent>())
            {
                for (var i = 0; i < 2; i++)
                {
                    var plank = Owner.EntityManager.SpawnEntity("WoodPlank1", Owner.Transform.Coordinates);
                    plank.RandomOffset(0.25f);
                }

                Owner.Delete();

                return true;
            }

            return false;
        }
    }
}
