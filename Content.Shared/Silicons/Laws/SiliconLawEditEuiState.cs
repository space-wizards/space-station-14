using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

[Serializable, NetSerializable]
public sealed class SiliconLawsEuiState : EuiStateBase
{
    public List<SiliconLaw> Laws { get; }
    public NetEntity Target { get; }
    public SiliconLawsEuiState(List<SiliconLaw> laws, NetEntity target)
    {
        Laws = laws;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class SiliconLawsSaveMessage : EuiMessageBase
{
    public List<SiliconLaw> Laws { get; }
    public LocId LawsetName;
    public NetEntity Target { get; }

    public SiliconLawsSaveMessage(List<SiliconLaw> laws, LocId lawsetName , NetEntity target)
    {
        Laws = laws;
        LawsetName = lawsetName;
        Target = target;
    }
}
