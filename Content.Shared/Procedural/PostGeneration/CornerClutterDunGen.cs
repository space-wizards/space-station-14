using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Spawns entities inside corners.
/// </summary>
public sealed partial class CornerClutterDunGen : IDunGenLayer
{
    [DataField]
    public float Chance = 0.50f;

    [DataField(required: true)]
    public EntityTableSelector Contents = default!;
}
