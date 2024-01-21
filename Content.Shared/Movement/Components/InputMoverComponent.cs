using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class InputMoverComponent : Component
    {
        // This class has to be able to handle server TPS being lower than client FPS.
        // While still having perfectly responsive movement client side.
        // We do this by keeping track of the exact sub-tick values that inputs are pressed on the client,
        // and then building a total movement vector based on those sub-tick steps.
        //
        // We keep track of the last sub-tick a movement input came in,
        // Then when a new input comes in, we calculate the fraction of the tick the LAST input was active for
        //   (new sub-tick - last sub-tick)
        // and then add to the total-this-tick movement vector
        // by multiplying that fraction by the movement direction for the last input.
        // This allows us to incrementally build the movement vector for the current tick,
        // without having to keep track of some kind of list of inputs and calculating it later.
        //
        // We have to keep track of a separate movement vector for walking and sprinting,
        // since we don't actually know our current movement speed while processing inputs.
        // We change which vector we write into based on whether we were sprinting after the previous input.
        //   (well maybe we do but the code is designed such that MoverSystem applies movement speed)
        //   (and I'm not changing that)

        /// <summary>
        /// Should our velocity be applied to our parent?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("toParent")]
        public bool ToParent = false;

        public GameTick LastInputTick;
        public ushort LastInputSubTick;

        public Vector2 CurTickWalkMovement;
        public Vector2 CurTickSprintMovement;

        public MoveButtons HeldMoveButtons = MoveButtons.None;

        /// <summary>
        /// Entity our movement is relative to.
        /// </summary>
        public EntityUid? RelativeEntity;

        /// <summary>
        /// Although our movement might be relative to a particular entity we may have an additional relative rotation
        /// e.g. if we've snapped to a different cardinal direction
        /// </summary>
        [ViewVariables]
        public Angle TargetRelativeRotation = Angle.Zero;

        /// <summary>
        /// The current relative rotation. This will lerp towards the <see cref="TargetRelativeRotation"/>.
        /// </summary>
        [ViewVariables]
        public Angle RelativeRotation;

        /// <summary>
        /// If we traverse on / off a grid then set a timer to update our relative inputs.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan LerpTarget;

        public const float LerpTime = 1.0f;

        public bool Sprinting => (HeldMoveButtons & MoveButtons.Walk) == 0x0;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanMove = true;
    }

    [Serializable, NetSerializable]
    public sealed class InputMoverComponentState : ComponentState
    {
        public MoveButtons HeldMoveButtons;
        public NetEntity? RelativeEntity;
        public Angle TargetRelativeRotation;
        public Angle RelativeRotation;
        public TimeSpan LerpTarget;
        public bool CanMove;
    }
}
