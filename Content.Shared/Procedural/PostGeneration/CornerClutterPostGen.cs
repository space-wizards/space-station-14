namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns entities inside corners.
/// </summary>
/// <remarks>
/// Dungeon data keys are:
/// - CornerClutter
/// </remarks>
public sealed partial class CornerClutterPostGen : IDunGenLayer
{
    [DataField]
    public float Chance = 0.50f;
}
