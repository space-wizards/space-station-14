using Content.Shared.Dragon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Component for the revolutionary supply rift.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RevSupplyRiftComponent : Component
{
    /// <summary>
    /// The current state of the rift.
    /// </summary>
    [DataField]
    public DragonRiftState State = DragonRiftState.Charging;
    
    /// <summary>
    /// The time when the rift was placed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan PlacedTime = TimeSpan.Zero;
    
    /// <summary>
    /// The current charge percentage of the rift (0-100).
    /// </summary>
    [DataField]
    public int ChargePercentage = 0;
    
    /// <summary>
    /// The name of the player who placed the rift.
    /// </summary>
    [DataField]
    public string PlacedBy = "Unknown";
}
