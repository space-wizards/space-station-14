using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.LawChips.Judge;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedJudgeInterfaceSystem))]
public sealed partial class JudgeInterfaceComponent : Component
{
    [DataField("status")]
    public JudgeInterfaceState Status = JudgeInterfaceState.Clean;

    [DataField("powered")]
    public bool Powered = false;
}

[Serializable, NetSerializable]
public enum JudgeInterfaceState : byte
{
    Clean,
    Busted,
    Hacked
}
