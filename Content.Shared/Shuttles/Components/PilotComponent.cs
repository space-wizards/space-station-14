using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Shuttles.Components
{
    /// <summary>
    /// Stores what shuttle this entity is currently piloting.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed partial class PilotComponent : Component
    {
        [ViewVariables]
        public EntityUid? Console { get; set; }

        /// <summary>
        /// Where we started piloting from to check if we should break from moving too far.
        /// </summary>
        [ViewVariables]
        public EntityCoordinates? Position { get; set; }

        public Vector2 CurTickStrafeMovement = Vector2.Zero;
        public float CurTickRotationMovement;
        public float CurTickBraking;

        public GameTick LastInputTick = GameTick.Zero;
        public ushort LastInputSubTick = 0;

        [ViewVariables]
        public ShuttleButtons HeldButtons = ShuttleButtons.None;

        public override bool SendOnlyToOwner => true;
    }
}
