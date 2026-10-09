using Robust.Shared.GameStates;

namespace Content.Shared.Wires;

/// <summary>
///     Allows hacking protections to a be added to an entity.
///     These safeguards are determined via a construction graph,
///     so the entity requires <cref="ConstructionComponent"/> for this to function
/// </summary>
[NetworkedComponent, RegisterComponent]
[Access(typeof(WiresSystem))]
[AutoGenerateComponentState]
public sealed partial class WiresPanelSecurityComponent : Component
{
    /// <summary>
    ///     A verbal description of the wire panel's current security level
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Examine;

    /// <summary>
    ///     Determines whether the wiring is accessible to hackers or not
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WiresAccessible = true;

    /// <summary>
    ///     Name of the construction graph node that the entity will start on
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SecurityLevel = string.Empty;
}

/// <summary>
///     This event gets raised when security settings on a wires panel change
/// </summary>
public sealed class WiresPanelSecurityEvent(string? examine, bool wiresAccessible) : EntityEventArgs
{
    public readonly string? Examine = examine;
    public readonly bool WiresAccessible = wiresAccessible;
}
