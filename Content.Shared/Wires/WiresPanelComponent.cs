using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Wires;

[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedWiresSystem))]
public sealed class WiresPanelComponent : Component
{
    /// <summary>
    ///     Is the panel open for this entity's wires?
    /// </summary>
    [ViewVariables]
    public bool Open;

    /// <summary>
    ///     Should this entity's wires panel be visible at all?
    /// </summary>
    [ViewVariables]
    public bool Visible = true;

    [DataField("screwdriverOpenSound")]
    public SoundSpecifier ScrewdriverOpenSound = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField("screwdriverCloseSound")]
    public SoundSpecifier ScrewdriverCloseSound = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");
}

[Serializable, NetSerializable]
public sealed class WiresPanelComponentState : ComponentState
{
    public bool Open;
    public bool Visible;
}
