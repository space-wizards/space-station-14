using System;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Clothing
{
    [NetworkedComponent()]
    public abstract class SharedMagbootsComponent : Component
    {
        public sealed override string Name => "Magboots";

        public abstract bool On { get; set; }

        protected void OnChanged()
        {
            // inventory system will automatically hook into the event raised by this and update accordingly
            if (Owner.TryGetContainer(out var container))
            {
                EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(container.Owner);
            }
        }
        [DataField("walkMoveCoeffecient", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public float WalkMoveCoeffecient = 0.85f;

        [DataField("sprintMoveCoeffecient", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SprintMoveCoeffecient = 0.65f;
        public float WalkSpeedModifier => On ? WalkMoveCoeffecient : 1;
        public float SprintSpeedModifier => On ? SprintMoveCoeffecient : 1;

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
