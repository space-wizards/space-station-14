using System.Text.RegularExpressions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Prototypes;

/// <summary>
/// Static reference to Regex that avoids the runtime cost of instantiating them.
/// </summary>
[Prototype]
public sealed partial class RegexPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public Regex Regex = new("", RegexOptions.Compiled);
}
