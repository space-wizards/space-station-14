using Robust.Shared.GameStates;

namespace Content.Shared.Wires;

/// <summary>
///     Allows hacking protections to a be added to an entity.
///     These safeguards are determined via a construction graph,
///     so the entity requires <cref="ConstructionComponent"/> for this to function 
/// </summary>
[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedWiresSystem))]
[AutoGenerateComponentState]
public sealed partial class WiresPanelSecurityComponent : Component
{
    /// <summary>
    ///     A verbal description of the wire panel's current security level
    /// </summary>
    [DataField("examine")]
    [AutoNetworkedField]
    public string? Examine = default!;

    /// <summary>
    ///     Determines whether the wiring is accessible to hackers or not
    /// </summary>
    [DataField("wiresAccessible")]
    [AutoNetworkedField]
    public bool WiresAccessible = true;

    /// <summary>
    ///     Determines whether the device can be welded shut or not
    /// </summary>
    /// <remarks>
    ///     Should be set false when you need to weld/unweld something to/from the wire panel
    /// </remarks>
    [DataField("weldingAllowed")]
    [AutoNetworkedField]
    public bool WeldingAllowed = true;

    /// <summary>
    ///     Name of the construction graph node that the entity will start on
    /// </summary>
    [DataField("securityLevel")]
    [AutoNetworkedField]
    public string SecurityLevel = string.Empty;
}

/// <summary>
///     This event gets raised when security settings on a wires panel change
/// </summary>
public sealed class WiresPanelSecurityEvent : EntityEventArgs
{
    public readonly string? Examine;
    public readonly bool WiresAccessible;
    public readonly bool WeldingAllowed;

    public WiresPanelSecurityEvent(string? examine, bool wiresAccessible, bool weldingAllowed)
    {
        Examine = examine;
        WiresAccessible = wiresAccessible;
        WeldingAllowed = weldingAllowed;
    }
}
