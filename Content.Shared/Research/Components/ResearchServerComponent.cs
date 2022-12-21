using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Research.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class ResearchServerComponent : Component
    {
        [DataField("servername"), ViewVariables(VVAccess.ReadWrite)]
        public string ServerName = "RDSERVER";

        [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
        public int Points;

        [ViewVariables(VVAccess.ReadOnly)]
        public int Id;

        /// <summary>
        /// Clients are not safe to read client-side
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public List<EntityUid> Clients = new();

        [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdateTime = TimeSpan.Zero;

        [DataField("researchConsoleUpdateTime"), ViewVariables(VVAccess.ReadWrite)]
        public readonly TimeSpan ResearchConsoleUpdateTime = TimeSpan.FromSeconds(1);
    }

    [Serializable, NetSerializable]
    public sealed class ResearchServerState : ComponentState
    {
        public string ServerName;
        public int Points;
        public int Id;
        public ResearchServerState(string serverName, int points, int id)
        {
            ServerName = serverName;
            Points = points;
            Id = id;
        }
    }

    /// <summary>
    /// Event raised on a server's clients when the point value of the server is changed.
    /// </summary>
    /// <param name="Server"></param>
    /// <param name="Total"></param>
    /// <param name="Delta"></param>
    [ByRefEvent]
    public readonly record struct ResearchServerPointsChangedEvent(EntityUid Server, int Total, int Delta);

    /// <summary>
    /// Event raised every second to calculate the amount of points added to the server.
    /// </summary>
    /// <param name="Server"></param>
    /// <param name="Points"></param>
    [ByRefEvent]
    public record struct ResearchServerGetPointsPerSecondEvent(EntityUid Server, int Points);
}
