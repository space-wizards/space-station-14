using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed partial class AdminCameraEuiState(NetEntity? camera, string name, GameTick tick) : EuiStateBase
{
    /// <summary>
    /// The camera entity we will use for the window.
    /// </summary>
    public readonly NetEntity? Camera = camera;

    /// <summary>
    /// The name of the observed entity.
    /// </summary>
    public readonly string Name = name;

    /// <summary>
    /// The current tick time, needed for cursed reasons.
    /// </summary>
    public readonly GameTick Tick = tick;
}

[Serializable, NetSerializable]
public sealed partial class AdminCameraFollowMessage : EuiMessageBase;
