using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    /// Amount of times in seconds it takes to open
    /// </summary>
    [DataField]
    public TimeSpan OpenDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The tool quality needed to open this panel.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> OpeningTool = "Screwing";

    /// <summary>
    /// Text showed on examine when the panel is closed.
    /// </summary>
    /// <returns></returns>
    [DataField]
    public LocId? ExamineTextClosed = "wires-panel-component-on-examine-closed";

    /// <summary>
    /// Text showed on examine when the panel is open.
    /// </summary>
    /// <returns></returns>
    [DataField]
    public LocId? ExamineTextOpen = "wires-panel-component-on-examine-open";
}

/// <summary>
/// Event raised on a <see cref="WiresPanelComponent"/> before its open state is about to be changed.
/// </summary>
[ByRefEvent]
public record struct AttemptChangePanelEvent(bool Open, EntityUid? User, bool Cancelled = false);

/// <summary>
/// Event raised when a panel is opened or closed.
/// </summary>
[ByRefEvent]
public readonly record struct PanelChangedEvent(bool Open);
