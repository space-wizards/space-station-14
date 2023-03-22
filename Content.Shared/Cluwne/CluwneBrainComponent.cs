using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cluwne;

[RegisterComponent]
[NetworkedComponent]
public sealed class CluwneBrainComponent : Component
{
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);
}
