using Content.Server.Tesla.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Tesla.Components;

/// <summary>
/// The component changes the visual of an object after it is struck by lightning
/// </summary>
[RegisterComponent, Access(typeof(LightningSparkingSystem)), AutoGenerateComponentPause]
public sealed partial class LightningSparkingComponent : Component
{
    /// <summary>
    /// Spark duration.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float LightningTime = 4;

    /// <summary>
    /// When the spark visual should turn off.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LightningEndTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsSparking;
}
