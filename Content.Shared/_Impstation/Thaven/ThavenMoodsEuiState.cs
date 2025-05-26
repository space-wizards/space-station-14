using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Thaven;

[Serializable, NetSerializable]
public sealed class ThavenMoodsEuiState : EuiStateBase
{
    public List<ThavenMood> Moods { get; }
    public bool FollowsShared { get; }
    public NetEntity Target { get; }
    public ThavenMoodsEuiState(List<ThavenMood> moods, bool followsShared, NetEntity target)
    {
        Moods = moods;
        FollowsShared = followsShared;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class ThavenMoodsSaveMessage : EuiMessageBase
{
    public List<ThavenMood> Moods { get; }
    public bool FollowShared { get; }
    public NetEntity Target { get; }

    public ThavenMoodsSaveMessage(List<ThavenMood> moods, bool followShared, NetEntity target)
    {
        Moods = moods;
        FollowShared = followShared;
        Target = target;
    }
}
