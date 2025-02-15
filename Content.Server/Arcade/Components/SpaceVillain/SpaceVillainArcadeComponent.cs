using Content.Server.Arcade.EntitySystems.SpaceVillain;

namespace Content.Server.Arcade.Components.SpaceVillain;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(SpaceVillainArcadeSystem))]
public sealed partial class SpaceVillainArcadeComponent : ArcadeComponent
{
    /// <summary>
    ///
    /// </summary>
    public byte PlayerHP = 0;

    /// <summary>
    ///
    /// </summary>
    public byte PlayerMP = 0;

    /// <summary>
    ///
    /// </summary>
    public string VillainName = "Villain";

    /// <summary>
    ///
    /// </summary>
    public byte VillainHP = 0;

    /// <summary>
    ///
    /// </summary>
    public byte VillainMP = 0;
}
