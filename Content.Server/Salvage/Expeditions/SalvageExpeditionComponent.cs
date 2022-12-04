using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Salvage.Expeditions;

/// <summary>
/// Designates this entity as holding a salvage expedition.
/// </summary>
[RegisterComponent]
public sealed class SalvageExpeditionComponent : Component
{
    /// <summary>
    /// When the expeditions ends.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan EndTime;

    [ViewVariables]
    public readonly List<EntityUid> SpawnMarkers = new();

    [ViewVariables]
    public string Faction = string.Empty;

    [ViewVariables]
    public string Config = string.Empty;

    /// <summary>
    /// Station whose mission this is.
    /// </summary>
    [ViewVariables]
    public EntityUid Station;

    [ViewVariables] public SalvagePhase Phase = SalvagePhase.Generating;
}

public enum SalvagePhase : byte
{
    Generating,
    Initializing,
    Initialized,
    Completed,
}
