using Content.Shared.Botany;
using Content.Shared.Botany.Systems;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component given to produce that lets seed extractors create seeds from them.
/// </summary>
[RegisterComponent, Access(typeof(PlantSeedsSystem))]
public sealed partial class ProduceComponent : Component
{
    [IncludeDataField]
    public SeedData Seed = new();
}
