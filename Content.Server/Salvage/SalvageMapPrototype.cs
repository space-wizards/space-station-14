using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Salvage;

[Prototype("salvageMap")]
public sealed partial class SalvageMapPrototype : IPrototype
{
    [ViewVariables] [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Relative directory path to the given map, i.e. `Maps/Salvage/template.yml`
    /// </summary>
    [DataField("mapPath", required: true)] public ResPath MapPath;

    /// <summary>
    /// Name for admin use
    /// </summary>
    [DataField("name")] public string Name = string.Empty;
}
