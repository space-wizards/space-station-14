using Robust.Shared.Prototypes;

namespace Content.Shared.Loadouts;

[Prototype("loadoutCategory")]
public sealed class LoadoutCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
}
