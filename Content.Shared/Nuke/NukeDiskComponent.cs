using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Nuke;

/// <summary>
/// Used for tracking the nuke disk.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NukeDiskComponent : Component
{
    /// <summary>
    /// Is this disk meant to be geofenced to a station?
    /// </summary>
    [DataField]
    public bool Geofence = true;

    /// <summary>
    /// How long should the disk be allowed to be off station before geofencing?
    /// </summary>
    [DataField]
    public TimeSpan OffStationTolerance = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Is the disk currently off its origin station?
    /// </summary>
    [DataField]
    public bool LeftStation = false;

    /// <summary>
    /// When the disk was most recently seen leaving the station.
    /// </summary>
    [DataField]
    public TimeSpan LeftStationWhen = TimeSpan.MaxValue;

    /// <summary>
    /// When the last popup for the disk offstation occured.
    /// </summary>
    [DataField]
    public TimeSpan LastPopup = TimeSpan.Zero;

    ///
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string TeleportFlare = "EffectFlashBluespace";

    /// <summary>
    ///     Origin map of this disk.
    /// </summary>
    [DataField]
    public EntityUid? OriginMap;

    /// <summary>
    ///     Origin grid of this disk.
    /// </summary>
    [DataField]
    public EntityUid? OriginGrid;
}
