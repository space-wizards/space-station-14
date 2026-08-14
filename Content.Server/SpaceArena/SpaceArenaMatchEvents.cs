using Content.Shared.SpaceArena;
using Content.Shared.Mobs;
using Robust.Shared.Network;

namespace Content.Server.SpaceArena;

[ByRefEvent]
public readonly record struct SpaceArenaMatchStateChangedEvent(
    SpaceArenaMatchState OldState,
    SpaceArenaMatchState NewState);

[ByRefEvent]
public readonly record struct SpaceArenaMatchPlayerJoinedEvent(NetUserId Player, EntityUid Mind);

[ByRefEvent]
public readonly record struct SpaceArenaMatchPlayerLeftEvent(NetUserId Player, EntityUid Mind);

[ByRefEvent]
public readonly record struct SpaceArenaMatchPlayerSpawnedEvent(
    NetUserId Player,
    EntityUid Mind,
    EntityUid Mob,
    string SpawnGroup);

[ByRefEvent]
public readonly record struct SpaceArenaMatchPlayerMobStateChangedEvent(
    NetUserId Player,
    EntityUid Mob,
    MobState NewMobState);
