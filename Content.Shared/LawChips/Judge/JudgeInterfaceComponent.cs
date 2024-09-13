using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.LawChips.Judge;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedJudgeInterfaceSystem))]
[AutoGenerateComponentState]
public sealed partial class JudgeInterfaceComponent : Component
{
    [DataField("status"), AutoNetworkedField]
    public JudgeInterfaceStatus Status = JudgeInterfaceStatus.Normal;  
}

[NetSerializable, Serializable]
public enum JudgeInterfaceStatus : byte
{
    Normal,
    Hacked,
    Broken
}
