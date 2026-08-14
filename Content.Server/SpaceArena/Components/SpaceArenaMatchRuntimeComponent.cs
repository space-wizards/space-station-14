using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server.SpaceArena.Components;

[RegisterComponent, Access(typeof(SpaceArenaMatchSystem), typeof(SpaceArenaDeathMatchSystem))]
public sealed partial class SpaceArenaMatchRuntimeComponent : Component
{
    public MapId Map = MapId.Nullspace;

    public EntityUid? Station;

    public readonly Dictionary<NetUserId, SpaceArenaMatchPlayerData> Players = [];

    public readonly Dictionary<NetUserId, SpaceArenaMatchSpectatorData> Spectators = [];

    public readonly Dictionary<string, List<EntityCoordinates>> SpawnPoints = [];

    public readonly Dictionary<string, int> NextSpawnPoints = [];

    public readonly List<EntityUid> Barriers = [];

    public readonly Dictionary<NetUserId, TimeSpan> Respawns = [];

    public readonly Dictionary<NetUserId, TimeSpan> DisconnectForfeits = [];

    public TimeSpan? NextRespawn;

    public TimeSpan? NextDisconnectForfeit;

    public bool CleanedUp;
}

public sealed class SpaceArenaMatchPlayerData
{
    public required EntityUid Mind;

    public EntityUid? LobbyEntity;

    public EntityUid? LobbyStation;

    public EntityUid? MatchEntity;

    public string SpawnGroup = Content.Shared.SpaceArena.SpaceArenaSpawnGroups.Player;
}

public sealed class SpaceArenaMatchSpectatorData
{
    public required EntityUid Mind;

    public EntityUid? LobbyStation;

    public EntityUid? SpectatorEntity;
}
