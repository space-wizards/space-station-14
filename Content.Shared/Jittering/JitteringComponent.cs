using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Jittering;

[Access(typeof(SharedJitteringSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JitteringComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Amplitude { get; set; }

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Frequency { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 LastJitter { get; set; }
}
