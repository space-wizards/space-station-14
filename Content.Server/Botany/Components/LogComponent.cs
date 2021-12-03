using System.Threading.Tasks;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
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

            if (eventArgs.Using.HasTag("BotanySharp"))
            {
                for (var i = 0; i < 2; i++)
                {
                    var plank = IoCManager.Resolve<IEntityManager>().SpawnEntity("MaterialWoodPlank1", IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates);
                    plank.RandomOffset(0.25f);
                }

                IoCManager.Resolve<IEntityManager>().QueueDeleteEntity((EntityUid) Owner);

                return true;
            }

            return false;
        }
    }
}
