using Robust.Shared.Audio;

namespace Content.Server.Wires;

[RegisterComponent]
public sealed class WiresComponent : Component
{
    /// <summary>
    ///     Is the panel open for this entity's wires?
    /// </summary>
    [ViewVariables]
    public bool IsPanelOpen { get; set; }

    /// <summary>
    ///     Should this entity's wires panel be visible at all?
    /// </summary>
    [ViewVariables]
    public bool IsPanelVisible { get; set; } = true;

    /// <summary>
    ///     The name of this entity's internal board.
    /// </summary>
    [DataField("BoardName")]
    public string BoardName { get; set; } = "Wires";

    /// <summary>
    ///     The layout ID of this entity's wires.
    /// </summary>
    [DataField("LayoutId", required: true)]
    public string LayoutId { get; set; } = default!;

    /// <summary>
    ///     The serial number of this board. Randomly generated upon start,
    ///     does not need to be set.
    /// </summary>
    [ViewVariables]
    public string? SerialNumber { get; set; }

    /// <summary>
    ///     The seed that dictates the wires appearance, as well as
    ///     the status ordering on the UI client side.
    /// </summary>
    [ViewVariables]
    public int WireSeed { get; set; }

    /// <summary>
    ///     The list of wires currently active on this entity.
    /// </summary>
    [ViewVariables]
    public List<Wire> WiresList { get; set; } = new();

    /// <summary>
    ///     Queue of wires saved while the wire's DoAfter event occurs, to prevent too much spam.
    /// </summary>
    [ViewVariables]
    public List<int> WiresQueue { get; } = new();

    /// <summary>
    ///     If this should follow the layout saved the first time the layout dictated by the
    ///     layout ID is generated, or if a new wire order should be generated every time.
    /// </summary>
    [DataField("alwaysRandomize")]
    public bool AlwaysRandomize { get; }

    /// <summary>
    ///     Marks if maintenance panel being open/closed by someone with a screwdriver.
    ///     Prevents do after spam.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsScrewing;

    /// <summary>
    ///     Per wire status, keyed by an object.
    /// </summary>
    [ViewVariables]
    public Dictionary<object, object> Statuses { get; } = new();

    /// <summary>
    ///     The state data for the set of wires inside of this entity.
    ///     This is so that wire objects can be flyweighted between
    ///     entities without any issues.
    /// </summary>
    [ViewVariables]
    public Dictionary<object, object> StateData { get; } = new();

    [DataField("pulseSound")]
    public SoundSpecifier PulseSound = new SoundPathSpecifier("/Audio/Effects/multitool_pulse.ogg");

    [DataField("screwdriverOpenSound")]
    public SoundSpecifier ScrewdriverOpenSound = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField("screwdriverCloseSound")]
    public SoundSpecifier ScrewdriverCloseSound = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");
}
