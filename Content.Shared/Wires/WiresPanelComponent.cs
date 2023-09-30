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
    ///     This prototype describes the current security features of the wire panel
    /// </summary>
    [DataField("securityLevel")]
    [ValidatePrototypeId<WiresPanelSecurityLevelPrototype>]
    [AutoNetworkedField]
    public string CurrentSecurityLevelID = "Level0";
}

/// <summary>
/// Event raised when a panel is opened or closed.
/// </summary>
[ByRefEvent]
public readonly record struct PanelChangedEvent(bool Open);
