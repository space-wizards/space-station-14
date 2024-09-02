namespace Content.Server.Salvage.Magnet;

[RegisterComponent]
public sealed partial class SalvageMagnetComponent : Component
{
    /// <summary>
    /// The max distance at which the magnet will pull in wrecks.
    /// Scales from 50% to 100%.
    /// </summary>
    [DataField]
    public float MagnetSpawnDistance = 128f;

    /// <summary>
    /// How far offset to either side will the magnet wreck spawn.
    /// </summary>
    [DataField]
    public float LateralOffset = 32f;
}
