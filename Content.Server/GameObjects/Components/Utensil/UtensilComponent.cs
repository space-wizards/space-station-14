using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Utensil;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Utensil
{
    [RegisterComponent]
    public class UtensilComponent : SharedUtensilComponent, IAfterInteract
    {
        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            if (!eventArgs.Target.TryGetComponent(out FoodComponent food))
            {
                return;
            }

            if (!InteractionChecks.InRangeUnobstructed(eventArgs))
            {
                return;
            }

            food.TryUseFood(eventArgs.User, null);
        }
    }
}
