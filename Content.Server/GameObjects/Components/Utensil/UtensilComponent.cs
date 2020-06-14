using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Utensil;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
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
        [Dependency] private readonly IEntitySystemManager _entitySystem;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        /// <summary>
        /// The chance that the utensil has to break with each use.
        /// A value of 0 means that it is unbreakable.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _breakChance;

        /// <summary>
        /// The sound to be played if the utensil breaks.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private string _breakSound;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _breakChance, "breakChance", 0);
            serializer.DataField(ref _breakSound, "breakSound", "/Audio/items/snap.ogg");
        }

        private void TryBreak(IEntity user)
        {
            if (_random.Prob(_breakChance))
            {
                _entitySystem.GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(_breakSound, user, AudioParams.Default.WithVolume(-2f));
                Owner.Delete();
            }
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.User == null || eventArgs.Target == null)
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
                TryBreak(eventArgs.User);
            }
        }
    }
}
