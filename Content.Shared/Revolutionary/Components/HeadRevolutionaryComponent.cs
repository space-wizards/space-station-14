using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Component used for marking a Head Rev for conversion and winning/losing.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class HeadRevolutionaryComponent : Component
{
    [DataField("headRevStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string HeadRevStatusIcon = "HeadRevolutionaryFaction";
}
