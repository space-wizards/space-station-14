using System.Collections.Generic;
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

        protected UtensilKind _kinds = UtensilKind.None;

        [ViewVariables]
        public override UtensilKind Kinds
        {
            get => _kinds;
            set
            {
                _kinds = value;
                Dirty();
            }
        }

        /// <summary>
        /// The chance that the utensil has to break with each use.
        /// A value of 0 means that it is unbreakable.
        /// </summary>
        [ViewVariables]
        private float _breakChance;

        /// <summary>
        /// The sound to be played if the utensil breaks.
        /// </summary>
        [ViewVariables]
        private string _breakSound;

        public void AddKind(UtensilKind kind)
        {
            Kinds |= kind;
        }

        public bool HasAnyKind(UtensilKind kind)
        {
            return (_kinds & kind) != UtensilKind.None;
        }

        public bool HasKind(UtensilKind kind)
        {
            return _kinds.HasFlag(kind);
        }

        public void RemoveKind(UtensilKind kind)
        {
            Kinds &= ~kind;
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                var kinds = serializer.ReadDataField("kinds", new List<UtensilKind>());
                foreach (var kind in kinds)
                {
                    AddKind(kind);
                }
            }

            serializer.DataField(ref _breakChance, "breakChance", 0);
            serializer.DataField(ref _breakSound, "breakSound", "/Audio/items/snap.ogg");
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
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

            food.TryUseFood(user, null, this);
        }
    }
}
