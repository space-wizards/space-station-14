using Robust.Shared.Prototypes;

namespace Content.Shared.Wires;

[Prototype("WiresPanelSecurityLevel")]
public sealed class WiresPanelSecurityLevelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("examine")]
    public string? Examine = default!;

    [DataField("wiresAccessible")]
    public bool WiresAccessible = true;
}
