using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

/// <summary>
/// Marker prototype that defines well-known types of organs, e.g. "kidneys" or "left arm".
/// </summary>
[Prototype]
public sealed partial class OrganCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
