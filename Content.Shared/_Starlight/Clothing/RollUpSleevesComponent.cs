using Content.Shared.Starlight.Clothing;
using Robust.Shared.GameStates;

namespace Content.Shared.Starlight.Clothing;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRollUpSleevesSystem)), AutoGenerateComponentState]
public sealed partial class RollUpSleevesComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Rolled;
}
