using System.Numerics;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.SpaceArena.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chat;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.SpaceArena;
using Content.Shared.SpaceArena.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SpaceArena;

public sealed partial class SpaceArenaDeathMatchSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SpaceArenaMatchSystem _matches = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    private static readonly SoundSpecifier VictorySound =
        new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg");
    private static readonly SoundSpecifier FightStartSound =
        new SoundPathSpecifier("/Audio/Weapons/boxingbell.ogg");
    private static readonly EntProtoId VictorySparkEffect = "EffectSparks";

    private const int VictorySparkCount = 6;
    private const float VictorySparkOffsetRange = 0.6f;
    private const float VictoryLightRadius = 5f;
    private const float VictoryLightEnergy = 4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceArenaDeathMatchComponent, SpaceArenaMatchPlayerLeftEvent>(OnPlayerLeft);
        SubscribeLocalEvent<SpaceArenaDeathMatchComponent, SpaceArenaMatchPlayerSpawnedEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<SpaceArenaDeathMatchComponent, SpaceArenaMatchStateChangedEvent>(OnMatchStateChanged);
        SubscribeLocalEvent<SpaceArenaDeathMatchComponent, SpaceArenaMatchPlayerMobStateChangedEvent>(
            OnMobStateChanged);
    }

    private void OnPlayerSpawned(
        Entity<SpaceArenaDeathMatchComponent> match,
        ref SpaceArenaMatchPlayerSpawnedEvent args)
    {
        _stationSpawning.EquipStartingGear(args.Mob, GetLoadout(match, args));
    }

    private void OnPlayerLeft(
        Entity<SpaceArenaDeathMatchComponent> match,
        ref SpaceArenaMatchPlayerLeftEvent args)
    {
        match.Comp.PlayerLoadouts.Remove(args.Player);

        if (_matches.IsMatchActive(match.Owner) &&
            TryComp(match.Owner, out SpaceArenaMatchRuntimeComponent? runtime))
        {
            FinishIfSoleActiveGroup(match, runtime, EntityUid.Invalid);
        }
    }

    private ProtoId<StartingGearPrototype> GetLoadout(
        Entity<SpaceArenaDeathMatchComponent> match,
        SpaceArenaMatchPlayerSpawnedEvent args)
    {
        if (match.Comp.PlayerLoadouts.TryGetValue(args.Player, out var assigned))
            return assigned;

        var loadout = match.Comp.Gear;
        if (TryComp(match.Owner, out SpaceArenaMatchComponent? matchData) &&
            matchData.Arena is { } arenaId &&
            ProtoMan.TryIndex(arenaId, out GameMapPrototype? arena) &&
            arena.SpaceArena is { } arenaData)
        {
            if (arenaData.Loadouts.Count > 0)
                loadout = GetGroupLoadout(match.Comp, args.SpawnGroup, arenaData.Loadouts);
            else if (arenaData.Loadout is { } arenaLoadout)
                loadout = arenaLoadout;
        }

        match.Comp.PlayerLoadouts.Add(args.Player, loadout);
        return loadout;
    }

    private ProtoId<StartingGearPrototype> GetGroupLoadout(
        SpaceArenaDeathMatchComponent component,
        string spawnGroup,
        List<ProtoId<StartingGearPrototype>> configured)
    {
        if (!component.GroupLoadouts.TryGetValue(spawnGroup, out var loadouts))
        {
            loadouts = new List<ProtoId<StartingGearPrototype>>(configured);
            _random.Shuffle(loadouts);
            component.GroupLoadouts.Add(spawnGroup, loadouts);
        }

        var index = component.NextGroupLoadout.GetValueOrDefault(spawnGroup) % loadouts.Count;
        component.NextGroupLoadout[spawnGroup] = index + 1;
        return loadouts[index];
    }

    private void OnMobStateChanged(
        Entity<SpaceArenaDeathMatchComponent> match,
        ref SpaceArenaMatchPlayerMobStateChangedEvent args)
    {
        if (args.NewMobState is not (MobState.Critical or MobState.Dead) ||
            !_matches.IsMatchActive(match.Owner) ||
            !TryComp(match.Owner, out SpaceArenaMatchRuntimeComponent? runtime))
        {
            return;
        }

        FinishIfSoleActiveGroup(match, runtime, args.Mob);
    }

    private void FinishIfSoleActiveGroup(
        Entity<SpaceArenaDeathMatchComponent> match,
        SpaceArenaMatchRuntimeComponent runtime,
        EntityUid eliminated)
    {
        if (!TryGetSoleActiveGroup(runtime, eliminated, out var winningGroup))
            return;

        match.Comp.WinningGroup = winningGroup;
        _matches.FinishMatch(match.Owner);
    }

    private bool TryGetSoleActiveGroup(
        SpaceArenaMatchRuntimeComponent runtime,
        EntityUid eliminated,
        out string? activeGroup)
    {
        activeGroup = null;

        foreach (var data in runtime.Players.Values)
        {
            if (data.MatchEntity is not { } mob ||
                mob == eliminated ||
                TerminatingOrDeleted(mob) ||
                EntityManager.IsQueuedForDeletion(mob) ||
                !_mobState.IsAlive(mob))
            {
                continue;
            }

            if (activeGroup == null)
            {
                activeGroup = data.SpawnGroup;
                continue;
            }

            if (activeGroup != data.SpawnGroup)
                return false;
        }

        return true;
    }

    private void OnMatchStateChanged(
        Entity<SpaceArenaDeathMatchComponent> match,
        ref SpaceArenaMatchStateChangedEvent args)
    {
        if (!TryComp(match.Owner, out SpaceArenaMatchRuntimeComponent? runtime))
            return;

        switch (args.NewState)
        {
            case SpaceArenaMatchState.Waiting:
                match.Comp.PlayerLoadouts.Clear();
                match.Comp.GroupLoadouts.Clear();
                match.Comp.NextGroupLoadout.Clear();
                match.Comp.WinningGroup = null;
                match.Comp.ResultAnnounced = false;
                break;
            case SpaceArenaMatchState.Countdown:
                if (TryComp(match.Owner, out SpaceArenaMatchComponent? matchData))
                {
                    SendToAll(
                        runtime,
                        Loc.GetString(
                            "space-arena-match-countdown",
                            ("seconds", (int) matchData.CountdownDuration.TotalSeconds)));
                }
                break;
            case SpaceArenaMatchState.Active:
                SendToAll(runtime, Loc.GetString("space-arena-match-fight-start"));
                PlayFightStartSound(runtime);
                FinishIfSoleActiveGroup(match, runtime, EntityUid.Invalid);
                break;
            case SpaceArenaMatchState.Ending:
                AnnounceResult(match, runtime);
                break;
        }
    }

    private void AnnounceResult(
        Entity<SpaceArenaDeathMatchComponent> match,
        SpaceArenaMatchRuntimeComponent runtime)
    {
        if (match.Comp.ResultAnnounced)
            return;

        match.Comp.ResultAnnounced = true;
        foreach (var (player, data) in runtime.Players)
        {
            var won = match.Comp.WinningGroup == data.SpawnGroup;
            var message = match.Comp.WinningGroup switch
            {
                null => Loc.GetString("space-arena-match-draw"),
                _ when won => Loc.GetString("space-arena-match-victory"),
                _ => Loc.GetString("space-arena-match-defeat"),
            };

            if (!_players.TryGetSessionById(player, out var session))
                continue;

            SendToPlayer(session, message);

            if (data.MatchEntity is not { } mob || TerminatingOrDeleted(mob))
                continue;

            _popup.PopupEntity(message, mob, session, PopupType.Large);

            if (!won || _mobState.IsDead(mob))
                continue;

            _audio.PlayGlobal(VictorySound, session);
            ApplyVictoryEffects(mob);
        }
    }

    private void ApplyVictoryEffects(EntityUid winner)
    {
        var light = _pointLight.EnsureLight(winner);
        _pointLight.SetColor(winner, Color.Gold, light);
        _pointLight.SetRadius(winner, VictoryLightRadius, light);
        _pointLight.SetEnergy(winner, VictoryLightEnergy, light);
        _pointLight.SetEnabled(winner, true, light);

        var coordinates = Transform(winner).Coordinates;
        for (var i = 0; i < VictorySparkCount; i++)
        {
            var offset = new Vector2(
                _random.NextFloat(-VictorySparkOffsetRange, VictorySparkOffsetRange),
                _random.NextFloat(-VictorySparkOffsetRange, VictorySparkOffsetRange));
            Spawn(VictorySparkEffect, coordinates.Offset(offset));
        }
    }

    private void SendToAll(SpaceArenaMatchRuntimeComponent runtime, string message)
    {
        foreach (var player in runtime.Players.Keys)
            SendToPlayer(player, message);
    }

    private void PlayFightStartSound(SpaceArenaMatchRuntimeComponent runtime)
    {
        foreach (var player in runtime.Players.Keys)
        {
            if (_players.TryGetSessionById(player, out var session))
                _audio.PlayGlobal(FightStartSound, session);
        }
    }

    private void SendToPlayer(NetUserId player, string message)
    {
        if (!_players.TryGetSessionById(player, out var session))
            return;

        SendToPlayer(session, message);
    }

    private void SendToPlayer(ICommonSession session, string message)
    {
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(
            ChatChannel.Server,
            message,
            wrapped,
            EntityUid.Invalid,
            false,
            session.Channel);
    }
}
