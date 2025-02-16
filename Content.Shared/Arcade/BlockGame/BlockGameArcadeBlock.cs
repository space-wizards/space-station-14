using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.BlockGame;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public enum BlockGameArcadeBlock : byte
{
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    LightBlue,
    Purple,
    GhostRed,
    GhostOrange,
    GhostYellow,
    GhostGreen,
    GhostBlue,
    GhostLightBlue,
    GhostPurple,
    Empty,
}
