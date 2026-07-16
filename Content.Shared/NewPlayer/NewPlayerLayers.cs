using Robust.Shared.Serialization;

namespace Content.Shared.NewPlayer;

/// <summary>
/// The enum for the new player indicator sprite.
/// </summary>
[Serializable, NetSerializable]
public enum NewPlayerLayers
{
    Layer,
}

/// <summary>
/// Enum to track which new player indicator should show.
/// </summary>
[Serializable, NetSerializable]
public enum NewPlayerVisuals
{
    NewTotal,
}
