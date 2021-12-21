using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public class LogComponent : Component, IInteractUsing
    {
        public override string Name => "Log";

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User))
                return false;

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (eventArgs.Using.HasTag("BotanySharp"))
            {
                for (var i = 0; i < 2; i++)
                {
                    var plank = entMan.SpawnEntity("MaterialWoodPlank1", entMan.GetComponent<TransformComponent>(Owner).Coordinates);
                    plank.RandomOffset(0.25f);
                }

                entMan.QueueDeleteEntity(Owner);

                return true;
            }

            return false;
        }
    }
}
