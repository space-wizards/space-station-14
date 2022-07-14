using Robust.Shared.Map;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to a component when it is queued or is travelling via FTL.
/// </summary>
[RegisterComponent]
public sealed class FTLComponent : Component
{
    [ViewVariables]
    public FTLState State = FTLState.Starting;

    [ViewVariables(VVAccess.ReadWrite)]
    public float StartupTime = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TravelTime = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator = 0f;

    /// <summary>
    /// Target Uid to dock with at the end of FTL.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("targetUid")]
    public EntityUid? TargetUid;

    [ViewVariables(VVAccess.ReadWrite), DataField("targetCoordinates")]
    public EntityCoordinates TargetCoordinates;
}

public enum FTLState : byte
{
    Starting,
    Travelling,
}
