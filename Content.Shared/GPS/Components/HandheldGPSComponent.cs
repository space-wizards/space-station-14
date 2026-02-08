using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.GPS.Components;

/// <summary>
/// This component adds coordinate displaying behavior to its owning entity. Specifically, the entity can be examined to
/// show its coordinates, or it can be held in hand and its title viewed to see the coordinates quickly. This component
/// supports various <see cref="HandheldGpsMode"/>s, which change exactly how its displayed coordinates are calculated
/// and what they're considered relative to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HandheldGPSComponent : Component
{
    /// <summary>
    /// How often the coordinates displayed on a held item's title are updated.
    /// </summary>
    [DataField]
    public float UpdateRate = 1.5f;

    /// <summary>
    /// Which <see cref="HandheldGpsMode">mode</see> this component is in, which determines
    /// how coordinates are calculated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HandheldGpsMode Mode = HandheldGpsMode.GridRelativeEntityCoordinates;

    /// <summary>
    /// The sound to play when changing <see cref="Mode"/>.
    /// </summary>
    [DataField]
    public SoundSpecifier? ModeChangeSound = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg");
}

/// <summary>
///  This enum describes what variety of coordinates a <see cref="HandheldGPSComponent"/> displays.
/// </summary>
[Serializable, NetSerializable]
public enum HandheldGpsMode
{
    /// <summary>
    /// "Absolute" coordinates within a map.
    /// </summary>
    MapCoordinates,

    /// <summary>
    /// "Relative" coordinates, based on whatever grid the entity is parented to.
    /// </summary>
    GridRelativeEntityCoordinates,
}
