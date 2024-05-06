namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns entities inside corners.
/// </summary>
public sealed partial class CornerClutterPostGen : IDunGenLayer
{
    [DataField]
    public float Chance = 0.50f;
}
