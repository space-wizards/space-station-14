using Robust.Shared.GameStates;

namespace Content.Shared.HijackBeacon;

/// <summary>
/// This is used for tracking a <see cref="HijackBeaconComponent"/> that is currently activated or on cooldown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class ActiveHijackBeaconComponent : Component
{
    /// <summary>
    ///     This timer determines both the timestamp when a hack ends, and when a cooldown ends depending on what mode the beacon is in.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    [Access(typeof(HijackBeaconSystem))]
    public TimeSpan CompletionTime = TimeSpan.Zero;
}
