using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Geofencing;

/// <summary>
/// Used to (try to) keep entities on their starting grids.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GeofencingComponent : Component
{
    /// <summary>
    /// Is geofencing active on this entity?
    /// </summary>
    [DataField]
    public bool Geofence = true;

    /// <summary>
    /// How long should the entity be allowed to be off station before geofencing?
    /// </summary>
    [DataField]
    public TimeSpan OffStationTolerance = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Is the entity currently off its origin station?
    /// </summary>
    [DataField]
    public bool LeftStation = false;

    /// <summary>
    /// When the entity was most recently seen leaving the station.
    /// </summary>
    [DataField]
    public TimeSpan LeftStationWhen = TimeSpan.MaxValue;

    /// <summary>
    /// When the last popup for the entity offstation occured.
    /// </summary>
    [DataField]
    public TimeSpan LastPopup = TimeSpan.Zero;

    /// <summary>
    /// A prototype effect to spawn where the fenced entity reappears.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string TeleportFlare = "EffectFlashBluespace";

    /// <summary>
    /// Origin map of this entity.
    /// </summary>
    [DataField]
    public EntityUid? OriginMap;

    /// <summary>
    /// Origin grid of this entity.
    /// </summary>
    [DataField]
    public EntityUid? OriginGrid;
}
