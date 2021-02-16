using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Utensil;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.DataClasses.Attributes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Utensil
{
    [RegisterComponent]
    [DataClass(typeof(UtensilComponentData))]
    public class UtensilComponent : SharedUtensilComponent, IAfterInteract
    {
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        protected UtensilType _types = UtensilType.None;

        [ViewVariables]
        [DataClassTarget("types")]
        public override UtensilType Types
        {
            get => _types;
            set
            {
                _types = value;
                Dirty();
            }
        }

        /// <summary>
        /// The chance that the utensil has to break with each use.
        /// A value of 0 means that it is unbreakable.
        /// </summary>
        [ViewVariables]
        [YamlField("breakChance")]
        private float _breakChance = default;

        /// <summary>
        /// The sound to be played if the utensil breaks.
        /// </summary>
        [ViewVariables]
        [YamlField("breakSound")]
        private string _breakSound = "/Audio/Items/snap.ogg";

        public void AddType(UtensilType type)
        {
            Types |= type;
        }

        public bool HasAnyType(UtensilType type)
        {
            return (_types & type) != UtensilType.None;
        }

        public bool HasType(UtensilType type)
        {
            return _types.HasFlag(type);
        }

        public void RemoveType(UtensilType type)
        {
            Types &= ~type;
        }

        internal void TryBreak(IEntity user)
        {
            if (_random.Prob(_breakChance))
            {
                _entitySystem.GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(_breakSound, user, AudioParams.Default.WithVolume(-2f));
                Owner.Delete();
            }
        }

        async Task IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            TryUseUtensil(eventArgs.User, eventArgs.Target);
        }

        private void TryUseUtensil(IEntity user, IEntity target)
        {
            if (user == null || target == null)
            {
                return;
            }

            if (!target.TryGetComponent(out FoodComponent food))
            {
                return;
            }

            if (!user.InRangeUnobstructed(target, popup: true))
            {
                return;
            }

            food.TryUseFood(user, null, this);
        }
    }
}
