using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Climbing;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClimbingComponent : Component
{
    /// <summary>
    /// Whether the owner is climbing on a climbable entity.
    /// </summary>
    [ViewVariables]
    public bool IsClimbing { get; set; }

    /// <summary>
    /// Whether the owner is being moved onto the climbed entity.
    /// </summary>
    [ViewVariables]
    public bool OwnerIsTransitioning { get; set; }

    /// <summary>
    ///     We'll launch the mob onto the table and give them at least this amount of time to be on it.
    /// </summary>
    public const float BufferTime = 0.3f;

    [ViewVariables]
    public Dictionary<string, int> DisabledFixtureMasks { get; } = new();

    [Serializable, NetSerializable]
    public sealed class ClimbModeComponentState : ComponentState
    {
        public ClimbModeComponentState(bool climbing, bool isTransitioning)
        {
            Climbing = climbing;
            IsTransitioning = isTransitioning;
        }

        public bool Climbing { get; }
        public bool IsTransitioning { get; }
    }
}
