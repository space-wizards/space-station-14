using Robust.Shared.Prototypes;

namespace Content.Shared.Wires;

[Prototype("WiresPanelSecurityLevel")]
public sealed class WiresPanelSecurityLevelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("examine")]
    public string? Examine = default!;

    [DataField("wiresAccessible")]
    public bool WiresAccessible = true;
}
