using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

[PrototypeRecord("body")]
public sealed record BodyPrototype(
    [field: IdDataField] string ID,
    string Name,
    Dictionary<string, string> Slots,
    Dictionary<string, HashSet<string>> Connections
) : IPrototype;
