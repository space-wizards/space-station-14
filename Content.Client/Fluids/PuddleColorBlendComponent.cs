using Robust.Client.Graphics;

namespace Content.Client.Fluids;

/// <summary>
/// Owns the shader state used to blend a puddle with its neighbors.
/// </summary>
[RegisterComponent, Access(typeof(PuddleSystem))]
public sealed partial class PuddleColorBlendComponent : Component
{
    /// <summary>
    /// North, north-east, east, south-east, south, south-west, west, north-west.
    /// </summary>
    [ViewVariables]
    public readonly Color[] NeighborColors = new Color[(int) PuddleNeighbor.Count];

    /// <summary>
    /// A value of one means the corresponding neighbor color is valid; zero means it is absent.
    /// Kept separate from alpha so transparent solution colors remain valid neighbors.
    /// </summary>
    [ViewVariables]
    public readonly float[] NeighborPresent = new float[(int) PuddleNeighbor.Count];

    [ViewVariables]
    public Color SelfColor = Color.White;

    public ShaderInstance? Shader;
}

/// <summary>
/// Indexes the neighbor arrays used by <see cref="PuddleColorBlendComponent"/> and its shader.
/// </summary>
public enum PuddleNeighbor : byte
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
    Count,
}
