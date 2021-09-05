using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;

namespace Content.Server.Plants.Components
{
    [RegisterComponent]
    public class PottedPlantHideComponent : Component, IInteractUsing, IInteractHand
    {
        public override string Name => "PottedPlantHide";

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (Owner.TryGetComponent<SecretStashECSComponent>(out var secretStash))
            {
                var secretStashSystem = Owner.EntityManager.EntitySysManager.GetEntitySystem<SecretStashSystem>();
                var isAnItemStashed = secretStashSystem.HasItemInside(secretStash);
                var args = new SecretStashTryHideItemEvent(eventArgs.User, Owner, eventArgs.Using);
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, args);

                return !isAnItemStashed;
            }

            return false;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (Owner.TryGetComponent<SecretStashECSComponent>(out var secretStash))
            {
                var secretStashSystem = Owner.EntityManager.EntitySysManager.GetEntitySystem<SecretStashSystem>();
                var gotItem = secretStashSystem.HasItemInside(secretStash);
                var args = new SecretStashTryGetItemEvent(eventArgs.User, Owner);
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, args);

                if (gotItem)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
