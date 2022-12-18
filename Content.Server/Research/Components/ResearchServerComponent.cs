using Content.Server.Research.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Research.Components
{
    [Access(typeof(ResearchSystem))]
    [RegisterComponent]
    public sealed class ResearchServerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] public string ServerName => _serverName;

        [DataField("servername")]
        private string _serverName = "RDSERVER";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("points")]
        public int Points = 0;

        [ViewVariables(VVAccess.ReadOnly)]
        public int Id { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public List<ResearchClientComponent> Clients { get; } = new();

        [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextUpdateTime = TimeSpan.Zero;

        [DataField("researchConsoleUpdateTime"), ViewVariables(VVAccess.ReadWrite)]
        public readonly TimeSpan ResearchConsoleUpdateTime = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Event raised on a server's clients when the point value of the server is changed.
    /// </summary>
    /// <param name="Server"></param>
    /// <param name="Total"></param>
    /// <param name="Delta"></param>
    [ByRefEvent]
    public record struct ResearchServerPointsChangedEvent(EntityUid Server, int Total, int Delta);

    /// <summary>
    /// Event raised every second to calculate the amount of points added to the server.
    /// </summary>
    /// <param name="Server"></param>
    /// <param name="Points"></param>
    [ByRefEvent]
    public record struct ResearchServerGetPointsPerSecondEvent(EntityUid Server, int Points);
}
