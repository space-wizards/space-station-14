using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MechThrustersComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool ThrustersEnabled = false;
    
    [ViewVariables(VVAccess.ReadWrite), DataField("drawRate")]
    public float DrawRate = 1f;
    
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}