using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing
{
    [NetworkedComponent()]
    public abstract class SharedMagbootsComponent : Component
    {
        [DataField("toggleAction", required: true)]
        public InstantAction ToggleAction = new();

        public abstract bool On { get; set; }

        protected void OnChanged()
        {
            EntitySystem.Get<SharedActionsSystem>().SetToggled(ToggleAction, On);

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
