using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Arena;

public static class ArenaConstants
{
    /// <summary>лок рас.</summary>
    public static readonly IReadOnlySet<string> SpeciesBlacklist = new HashSet<string> { "IPC", "Vox" };

    /// <summary>Валюта. Пока не нужна, не настроено сохранение</summary>
    public const int KillCurrencyReward = 1;
}

[Serializable, NetSerializable]
public sealed class ArenaJoinEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ArenaLeaveEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ArenaPickEvent : EntityEventArgs
{
    public int Pick { get; }

    public ArenaPickEvent(int pick)
    {
        Pick = pick;
    }
}

[Serializable, NetSerializable]
public sealed class ArenaPlayerRecord
{
    public string PlayerName = "";
    public int Kills;
    public int Deaths;
    public double KD;
}

/// <summary>
/// Итоги арены за раунд. Рассылается сервером при окончании раунда.
/// </summary>
[Serializable, NetSerializable]
public sealed class ArenaManifestEvent : EntityEventArgs
{
    /// <summary>Игроки арены.</summary>
    public List<ArenaPlayerRecord> Players = new();
}
