using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Salvage;

[Prototype]
public sealed class SalvageMapPrototype : IPrototype
{
    [ViewVariables] [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Relative directory path to the given map, i.e. `Maps/Salvage/template.yml`
    /// </summary>
    [DataField(required: true)] public ResPath MapPath;
}
