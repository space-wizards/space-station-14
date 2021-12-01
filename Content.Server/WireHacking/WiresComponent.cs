using System.Collections.Generic;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Wires;

[RegisterComponent]
public class WiresComponent : Component
{
    public override string Name { get; } = "Wires";

    [ViewVariables]
    public bool IsPanelOpen { get; set; }

    [ViewVariables]
    public bool IsPanelVisible { get; set; }

    [ViewVariables]
    [DataField("BoardName")]
    public string BoardName { get; set; } = "Wires";

    [ViewVariables]
    [DataField("LayoutId")]
    public string? LayoutId { get; set; }

    [ViewVariables]
    public string? SerialNumber { get; set; }

    [ViewVariables]
    public int WireSeed { get; set; }

    [ViewVariables]
    public List<Wire> WiresList { get; } = new();

    [ViewVariables]
    [DataField("wireActions", required: true)]
    public List<IWireAction> WireActions { get; } = new();

    [ViewVariables]
    public Dictionary<object, object> Statuses { get; } = new();

    [ViewVariables]
    public Dictionary<object, object> StateData { get; } = new();

    [DataField("pulseSound")]
    public SoundSpecifier PulseSound = new SoundPathSpecifier("/Audio/Effects/multitool_pulse.ogg");

    [DataField("screwdriverOpenSound")]
    public SoundSpecifier ScrewdriverOpenSound = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

    [DataField("screwdriverCloseSound")]
    public SoundSpecifier ScrewdriverCloseSound = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");


}
