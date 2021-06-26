#nullable enable
using System;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing
{
    [NetworkedComponent()]
    public abstract class SharedMagbootsComponent : Component, IMoveSpeedModifier
    {
        public sealed override string Name => "Magboots";

        public abstract bool On { get; set; }


        protected void OnChanged()
        {
            MovementSpeedModifierComponent.RefreshItemModifiers(Owner);
        }

        public float WalkSpeedModifier => On ? 0.85f : 1;
        public float SprintSpeedModifier => On ? 0.65f : 1;

        [Serializable, NetSerializable]
        public sealed class MagbootsComponentState : ComponentState
        {
            public bool On { get; }

            public MagbootsComponentState(bool @on)
            {
                On = on;
            }
        }
    }
}
