using Content.Shared.Dragon;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Revolutionary.Components;

/// <summary>
/// Component for the revolutionary supply rift.
/// </summary>
[RegisterComponent]
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
}
