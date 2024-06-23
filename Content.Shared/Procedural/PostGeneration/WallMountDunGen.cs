namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns on the boundary tiles of rooms.
/// </summary>
public sealed partial class WallMountDunGen : IDunGenLayer
{
    /// <summary>
    /// Chance per free tile to spawn a wallmount.
    /// </summary>
    [DataField]
    public double Prob = 0.1;
}
