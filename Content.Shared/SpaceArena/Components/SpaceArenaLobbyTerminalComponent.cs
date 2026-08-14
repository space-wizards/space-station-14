using Content.Shared.Maps;
using Content.Shared.SpaceArena;
using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SpaceArena.Components;

[Serializable, NetSerializable]
public sealed class SpaceArenaOpenLobbyRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class SpaceArenaLeaveSpectatingRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class SpaceArenaLeaveMatchRequest : EntityEventArgs;

[Serializable, NetSerializable]
public enum SpaceArenaLobbyUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SpaceArenaLobbyBoundUserInterfaceState(
    List<SpaceArenaLobbyModeOption> modes,
    List<SpaceArenaLobbyArenaOption> arenas,
    List<SpaceArenaLobbyRoom> rooms) : BoundUserInterfaceState
{
    public readonly List<SpaceArenaLobbyModeOption> Modes = modes;
    public readonly List<SpaceArenaLobbyArenaOption> Arenas = arenas;
    public readonly List<SpaceArenaLobbyRoom> Rooms = rooms;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaLobbyModeOption(EntProtoId id, LocId name)
{
    public readonly EntProtoId Id = id;
    public readonly LocId Name = name;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaLobbyArenaOption(
    ProtoId<GameMapPrototype> id,
    string name,
    string format,
    EntProtoId? previewWeapon,
    int minPlayers,
    int maxPlayers,
    List<EntProtoId> modes)
{
    public readonly ProtoId<GameMapPrototype> Id = id;
    public readonly string Name = name;
    public readonly string Format = format;
    public readonly EntProtoId? PreviewWeapon = previewWeapon;
    public readonly int MinPlayers = minPlayers;
    public readonly int MaxPlayers = maxPlayers;
    public readonly List<EntProtoId> Modes = modes;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaLobbyRoom(
    NetEntity lobby,
    NetUserId host,
    string hostName,
    LocId modeName,
    string arenaName,
    int playerCount,
    int minPlayers,
    int maxPlayers,
    SpaceArenaMatchState state)
{
    public readonly NetEntity Lobby = lobby;
    public readonly NetUserId Host = host;
    public readonly string HostName = hostName;
    public readonly LocId ModeName = modeName;
    public readonly string ArenaName = arenaName;
    public readonly int PlayerCount = playerCount;
    public readonly int MinPlayers = minPlayers;
    public readonly int MaxPlayers = maxPlayers;
    public readonly SpaceArenaMatchState State = state;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaCreateLobbyMessage(EntProtoId mode, ProtoId<GameMapPrototype> arena)
    : BoundUserInterfaceMessage
{
    public readonly EntProtoId Mode = mode;
    public readonly ProtoId<GameMapPrototype> Arena = arena;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaJoinLobbyMessage(NetEntity lobby) : BoundUserInterfaceMessage
{
    public readonly NetEntity Lobby = lobby;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaStartLobbyMessage(NetEntity lobby) : BoundUserInterfaceMessage
{
    public readonly NetEntity Lobby = lobby;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaSpectateLobbyMessage(NetEntity lobby) : BoundUserInterfaceMessage
{
    public readonly NetEntity Lobby = lobby;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaLeaveLobbyMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SpaceArenaLobbyStatusRequestMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SpaceArenaLobbyUserStatusMessage(
    NetEntity? currentLobby,
    NetEntity? spectatedMatch,
    bool canManageLobbies) : BoundUserInterfaceMessage
{
    public readonly NetEntity? CurrentLobby = currentLobby;
    public readonly NetEntity? SpectatedMatch = spectatedMatch;
    public readonly bool CanManageLobbies = canManageLobbies;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaLobbyEuiState(
    List<SpaceArenaLobbyModeOption> modes,
    List<SpaceArenaLobbyArenaOption> arenas,
    List<SpaceArenaLobbyRoom> rooms,
    NetEntity? currentLobby,
    NetEntity? spectatedMatch,
    bool canManageLobbies) : EuiStateBase
{
    public readonly List<SpaceArenaLobbyModeOption> Modes = modes;
    public readonly List<SpaceArenaLobbyArenaOption> Arenas = arenas;
    public readonly List<SpaceArenaLobbyRoom> Rooms = rooms;
    public readonly NetEntity? CurrentLobby = currentLobby;
    public readonly NetEntity? SpectatedMatch = spectatedMatch;
    public readonly bool CanManageLobbies = canManageLobbies;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaCreateLobbyEuiMessage(
    EntProtoId mode,
    ProtoId<GameMapPrototype> arena) : EuiMessageBase
{
    public readonly EntProtoId Mode = mode;
    public readonly ProtoId<GameMapPrototype> Arena = arena;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaJoinLobbyEuiMessage(NetEntity lobby) : EuiMessageBase
{
    public readonly NetEntity Lobby = lobby;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaStartLobbyEuiMessage(NetEntity lobby) : EuiMessageBase
{
    public readonly NetEntity Lobby = lobby;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaSpectateLobbyEuiMessage(NetEntity lobby) : EuiMessageBase
{
    public readonly NetEntity Lobby = lobby;
}

[Serializable, NetSerializable]
public sealed class SpaceArenaLeaveLobbyEuiMessage : EuiMessageBase;
