using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CloudEmote;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CloudEmotePhaseComponent : Component
{
    public EntityUid? Player;
}
