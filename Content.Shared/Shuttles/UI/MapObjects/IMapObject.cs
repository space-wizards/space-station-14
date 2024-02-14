namespace Content.Shared.Shuttles.UI.MapObjects;

/// <summary>
/// Abstract map object representing a grid, beacon etc for use on the map screen.
/// </summary>
public interface IMapObject
{
    string Name { get; }
}
