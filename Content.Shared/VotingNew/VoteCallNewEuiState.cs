using Content.Shared.Eui;
using Content.Shared.Procedural;
using Robust.Shared.Serialization;

namespace Content.Shared.VotingNew;

[Serializable, NetSerializable]
public sealed class VoteCallNewEuiState : EuiStateBase
{

    public readonly Dictionary<string, string> Presets;
    public readonly Dictionary<string, string> PresetsTypes;

    public VoteCallNewEuiState(Dictionary<string, string> presets, Dictionary<string, string> presetsTypes)
    {
        Presets = presets;
        PresetsTypes = presetsTypes;
    }
}

public static class VoteCallNewEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class DoVote : EuiMessageBase
    {
        public List<string> TargetPresetList = new();
    }
}
