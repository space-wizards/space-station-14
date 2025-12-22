using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Research.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ResearchServerComponent : Component
{
    /// <summary>
    /// The name of the server
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public string ServerName = "RDSERVER";

    /// <summary>
    /// The amount of points on the server.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public int Points;

    /// <summary>
    /// Cost of technology research options reroll.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public int RediscoverCost = 2000;

    /// <summary>
    /// A unique numeric id representing the server
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int Id;

    /// <summary>
    /// Entities connected to the server
    /// </summary>
    /// <remarks>
    /// This is not safe to read clientside
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Clients = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan ResearchConsoleUpdateTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time when next reroll for tech to research will be available.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRediscover;

    /// <summary>
    /// Minimal interval between rediscover actions.
    /// </summary>
    [DataField]
    public TimeSpan RediscoverInterval = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Event raised on a server's clients when the point value of the server is changed.
/// </summary>
[ByRefEvent]
public readonly record struct ResearchServerPointsChangedEvent(EntityUid Server, int Total, int Delta);

/// <summary>
/// Event raised every second to calculate the amount of points added to the server.
/// </summary>
[ByRefEvent]
public record struct ResearchServerGetPointsPerSecondEvent(EntityUid Server, int Points);

