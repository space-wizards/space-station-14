using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Starlight.CloudEmote;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class CloudEmotePhaseComponent : Component
{
    [DataField("player"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? Player;
}
