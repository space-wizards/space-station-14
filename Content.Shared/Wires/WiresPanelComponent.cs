using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Wires;

[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedWiresSystem))]
[AutoGenerateComponentState]
public sealed partial class WiresPanelComponent : Component
{
    /// <summary>
    ///     Is the panel open for this entity's wires?
    /// </summary>
    [DataField("open")]
    [AutoNetworkedField]
    public bool Open;

    /// <summary>
    ///     Should this entity's wires panel be visible at all?
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public bool Visible = true;

    [DataField("screwdriverOpenSound")]
    public SoundSpecifier ScrewdriverOpenSound = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField("screwdriverCloseSound")]
    public SoundSpecifier ScrewdriverCloseSound = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");

    /// <summary>
    ///     A verbal description of the wire panel's current security level
    /// </summary>
    [AutoNetworkedField]
    public string? WiresPanelSecurityExamination = default!;

    /// <summary>
    ///     Determines whether the wiring is accessible to hackers or not
    /// </summary>
    [AutoNetworkedField]
    public bool WiresAccessible = true;

    /// <summary>
    ///     Determines whether the device can be welded shut or not
    /// </summary>
    /// <remarks>
    ///     Should be set false when you need to weld/unweld something to/from the wire panel
    /// </remarks>
    [AutoNetworkedField]
    public bool WeldingAllowed = true;
}

/// <summary>
/// Event raised when a panel is opened or closed.
/// </summary>
[ByRefEvent]
public readonly record struct PanelChangedEvent(bool Open);
