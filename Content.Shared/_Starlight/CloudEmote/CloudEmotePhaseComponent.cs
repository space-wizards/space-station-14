using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Starlight.CloudEmote;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CloudEmotePhaseComponent : Component
{
    public EntityUid? Player;
}
