#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Culinary
{
    [RegisterComponent]
    public class UtensilComponent : Component, IAfterInteract
    {
        public override string Name => "Utensil";

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
        private float _breakChance;

        /// <summary>
        /// The sound to be played if the utensil breaks.
        /// </summary>
        [ViewVariables]
        private string? _breakSound;

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
            if (_breakSound != null && IoCManager.Resolve<IRobustRandom>().Prob(_breakChance))
            {
                EntitySystem.Get<AudioSystem>()
                    .PlayFromEntity(_breakSound, user, AudioParams.Default.WithVolume(-2f));
                Owner.Delete();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("types",
                new List<UtensilType>(),
                types => types.ForEach(AddType),
                () =>
                {
                    var types = new List<UtensilType>();

                    foreach (UtensilType type in Enum.GetValues(typeof(UtensilType)))
                    {
                        if ((Types & type) != 0)
                        {
                            types.Add(type);
                        }
                    }

                    return types;
                });

            serializer.DataField(ref _breakChance, "breakChance", 0);
            serializer.DataField(ref _breakSound, "breakSound", "/Audio/Items/snap.ogg");
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            TryUseUtensil(eventArgs.User, eventArgs.Target);
            return true;
        }

        private void TryUseUtensil(IEntity user, IEntity? target)
        {
            if (target == null || !target.TryGetComponent(out FoodComponent? food))
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

    [Flags]
    public enum UtensilType : byte
    {
        None = 0,
        Fork = 1,
        Spoon = 1 << 1,
        Knife = 1 << 2
    }
}
