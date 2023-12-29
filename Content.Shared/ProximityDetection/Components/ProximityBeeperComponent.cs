using Content.Shared.ProximityDetection.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ProximityDetection.Components;

/// <summary>
/// This is used for an item that beeps based on
/// proximity to a specified component.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ProximityBeeperSystem)), AutoGenerateComponentState]
public sealed partial class ProximityBeeperComponent : Component
{
    /// <summary>
    /// Whether or not it's on.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;

    /// <summary>
    /// Whether to draw power while enabled
    /// </summary>
    [DataField("drawsPower"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool DrawsPower = true;

    /// <summary>
    /// Found Entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? TargetEnt;

    /// <summary>
    /// Distance to Found Entity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Distance = -1;

    /// <summary>
    /// The farthest distance a target can be for the beep to occur
    /// </summary>
    [DataField("maximumDistance"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaximumDistance = 10f;

    /// <summary>
    /// The maximum interval between beeps.
    /// </summary>
    [DataField("maxBeepInterval"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MaxBeepInterval = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// The minimum interval between beeps.
    /// </summary>
    [DataField("minBeepInterval"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan MinBeepInterval = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// When the next beep will occur
    /// </summary>
    [DataField("nextBeepTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan NextBeepTime;

    /// <summary>
    /// Is the beep muted
    /// </summary>
    [DataField("muted"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool IsMuted = false;

    /// <summary>
    /// The sound played when the locator beeps.
    /// </summary>
    [DataField("beepSound"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public SoundSpecifier? BeepSound;
}
