using Robust.Shared.Serialization;

namespace Content.Shared.NewPlayer;

/// <summary>
/// Enum to track which new player indicator should show.
/// </summary>
[Serializable, NetSerializable]
public enum NewPlayerVisuals
{
    NewTotal,
}
