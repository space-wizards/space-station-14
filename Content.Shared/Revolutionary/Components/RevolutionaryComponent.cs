using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Used for marking regular revs as well as storing icon prototypes so you can see fellow revs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class RevolutionaryComponent : Component
{
    [DataField("RevStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string RevStatusIcon = "RevolutionaryFaction";
}
