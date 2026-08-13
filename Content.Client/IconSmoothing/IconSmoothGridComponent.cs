namespace Content.Client.IconSmoothing;

/// <summary>
/// This is used to cache Icon Smoothing data for a grid for the <see cref="IconSmoothComponent"/>
/// This is applied to a grid when an <see cref="IconSmoothComponent"/> entity is anchored to the grid.
/// </summary>
[RegisterComponent]
public sealed partial class IconSmoothGridComponent : Component
{
    /// <summary>
    /// Data for every tile with an anchored <see cref="IconSmoothComponent"/> on the grid.
    /// Stores an integer which corresponds to a cache for similar <see cref="IconSmoothComponent.Key"/> Hashsets.
    /// </summary>
    /// <remarks>
    /// Intentionally not saved.
    /// If you need more than 256 possible different key states, then you may have a problem, change to ushort instead:tm:
    /// </remarks>
    [ViewVariables]
    public readonly Dictionary<Vector2i, byte> Tiles = new();
}
