#nullable enable
using System;
using System.Threading.Tasks;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    public class UtensilComponent : Component, IAfterInteract
    {
        public override string Name => "Utensil";

        [DataField("types")]
        private UtensilType _types = UtensilType.None;

        [ViewVariables]
        public UtensilType Types
        {
            get => _types;
            set
            {
                if (_types.Equals(value))
                    return;

                _types = value;
            }
        }

        /// <summary>
        /// The chance that the utensil has to break with each use.
        /// A value of 0 means that it is unbreakable.
        /// </summary>
        [ViewVariables]
        [DataField("breakChance")]
        private float _breakChance;

        /// <summary>
        /// The sound to be played if the utensil breaks.
        /// </summary>
        [ViewVariables]
        [DataField("breakSound")]
        private SoundSpecifier _breakSound = new SoundPathSpecifier("/Audio/Items/snap.ogg");

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
            return (_types & type) != 0;
        }

        public void RemoveType(UtensilType type)
        {
            Types &= ~type;
        }

        internal void TryBreak(IEntity user)
        {
            if (_breakSound.TryGetSound(out var breakSound) && IoCManager.Resolve<IRobustRandom>().Prob(_breakChance))
            {
                SoundSystem.Play(Filter.Pvs(user), breakSound, user, AudioParams.Default.WithVolume(-2f));
                Owner.Delete();
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            return TryUseUtensil(eventArgs.User, eventArgs.Target);
        }

        private bool TryUseUtensil(IEntity user, IEntity? target)
        {
            if (target == null || !target.TryGetComponent(out FoodComponent? food))
            {
                return false;
            }

            if (!user.InRangeUnobstructed(target, popup: true))
            {
                return false;
            }

            food.TryUseFood(user, null, this);
            return true;
        }
    }

    [Flags]
    public enum UtensilType : byte
    {
        None = 0,
        Fork = 1,
        Spoon = 1 << 1,
        Knife = 1 << 2
    }
}
