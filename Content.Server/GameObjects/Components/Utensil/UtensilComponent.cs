using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Utensil;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Utensil
{
    [RegisterComponent]
    public class UtensilComponent : SharedUtensilComponent, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        /// <summary>
        /// The chance that the utensil has to break with each use.
        /// A value of 0 means that it is unbreakable.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _breakChance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _breakChance, "breakChance", 0);
        }

        private void TryBreak()
        {
            if (_random.Prob(_breakChance))
            {
                Owner.Delete();
            }
        }

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

            if (food.TryUseFood(eventArgs.User, null))
            {
                TryBreak();
            }
        }
    }
}
