using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Revolutionary;

[RegisterComponent, NetworkedComponent]

public sealed class RevolutionaryComponent : Component
{
    [DataField("RevStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string RevStatusIcon = "RevolutionaryFaction";

    [DataField("headRevStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string HeadRevStatusIcon = "HeadRevolutionaryFaction";
}
