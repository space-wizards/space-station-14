using Robust.Shared.Serialization;

namespace Content.Shared.Toggleable;

// Appearance Data key
[Serializable, NetSerializable]
public enum ToggleableLightVisuals : byte
{
    Enabled,
    Color
}

/// <summary>
///     Generic sprite layer keys.
/// </summary>
[Serializable, NetSerializable]
public enum LightLayers : byte
{
    Light,

    /// <summary>
    ///     Used as a key for generic unshaded layers. Not necessarily related to an entity with an actual light source.
    ///     Use this instead of creating a unique single-purpose "unshaded" enum for every visualizer. 
    /// </summary>
    Unshaded,
}
