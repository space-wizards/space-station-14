using Robust.Shared.GameStates;

namespace Content.Shared.Climbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClimbingComponent : Component
{
    /// <summary>
    /// Whether the owner is climbing on a climbable entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsClimbing { get; set; }

    /// <summary>
    /// Whether the owner is being moved onto the climbed entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool OwnerIsTransitioning { get; set; }

    /// <summary>
    ///     We'll launch the mob onto the table and give them at least this amount of time to be on it.
    /// </summary>
    public const float BufferTime = 0.3f;

    [ViewVariables]
    public Dictionary<string, int> DisabledFixtureMasks { get; } = new();
}
