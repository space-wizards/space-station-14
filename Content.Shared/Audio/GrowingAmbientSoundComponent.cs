using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Audio;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedAmbientSoundSystem))]
public sealed partial class GrowingAmbientSoundComponent : Component
{
    /// <summary>
    /// Minimum range to which the sound range can decrease.
    /// </summary>
    [DataField]
    public float MinRange;

    /// <summary>
    /// Maximum range to which the sound range can increase.
    /// </summary>
    [DataField]
    public float MaxRange = 15f;

    /// <summary>
    /// How much to increase the range by every <see cref="GrowthDelay"/>.
    /// </summary>
    [DataField]
    public float? RangeGrowth = 0f;

    /// <summary>
    /// Minimum volume to which the sound volume can decrease.
    /// </summary>
    [DataField]
    public float MinVolume;

    /// <summary>
    /// Maximum volume to which the sound volume can increase.
    /// </summary>
    [DataField]
    public float MaxVolume = 15f;

    /// <summary>
    /// How much to increase the volume by every <see cref="GrowthDelay"/>.
    /// </summary>
    [DataField]
    public float? VolumeGrowth = 0f;

    /// <summary>
    /// How often the range and volume should grow by their respective <see cref="VolumeGrowth"/> and <see cref="RangeGrowth"/>.
    /// </summary>
    [DataField]
    public TimeSpan GrowthDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Next time the range/volume will increase.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextGrowth = TimeSpan.Zero;
}
