using Content.Shared._Starlight.Clothing;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Clothing;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRollUpSleevesSystem)), AutoGenerateComponentState]
public sealed partial class RollUpSleevesComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Rolled;
}
