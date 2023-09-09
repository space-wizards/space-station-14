using Robust.Shared.Prototypes;

namespace Content.Shared.Wires;

[Prototype("WiresPanelSecurityLevel")]
public sealed class WiresPanelSecurityLevelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     A verbal description of the wire panel's current security level
    /// </summary>
    [DataField("examine")]
    public string? Examine = default!;

    /// <summary>
    ///     Determines whether the wiring is accessible to hackers or not
    /// </summary>
    [DataField("wiresAccessible")]
    public bool WiresAccessible = true;

    /// <summary>
    ///     Determines whether the device can be welded shut or not
    /// </summary>
    /// <remarks>
    ///     Should be set false when you need to weld/unweld something to/from the wire panel
    /// </remarks>
    [DataField("weldingAllowed")]
    public bool WeldingAllowed = true;
}
