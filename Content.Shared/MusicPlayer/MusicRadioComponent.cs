using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameObjects;

namespace Content.Shared.MusicPlayer;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class MusicRadioComponent : Component
{
}
