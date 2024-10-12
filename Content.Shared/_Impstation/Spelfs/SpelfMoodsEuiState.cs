using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Spelfs;

[Serializable, NetSerializable]
public sealed class SpelfMoodsEuiState : EuiStateBase
{
    public List<SpelfMood> Moods { get; }
    public NetEntity Target { get; }
    public SpelfMoodsEuiState(List<SpelfMood> moods, NetEntity target)
    {
        Moods = moods;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class SpelfMoodsSaveMessage : EuiMessageBase
{
    public List<SpelfMood> Moods { get; }
    public NetEntity Target { get; }

    public SpelfMoodsSaveMessage(List<SpelfMood> moods, NetEntity target)
    {
        Moods = moods;
        Target = target;
    }
}
