using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.DeadSpace.Prison;
using Content.Server.Chat.Managers;
using Content.Shared.Armor;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.DeadSpace.Arena;
using Content.Shared.Fluids.Components;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.Arena;

public sealed class ArenaSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly GhostSystem _ghosts = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IRobustRandom _luck = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly SharedStationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly PrisonSystem _prison = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedArmorSystem _armor = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const string ArenaMapFile = "/Maps/_DeadSpace/arena.yml";

    public bool Enabled { get; private set; } = true;

    internal bool CanJoinArena(ICommonSession session)
    {
        return Enabled && !_prison.IsUserPrisoner(session.UserId);
    }

    public void ToggleEnabled()
    {
        Enabled = !Enabled;
    }

    private EntityUid? _arenaMap;
    private readonly HashSet<NetEntity> _roster = new();
    private readonly List<ArenaLoadoutPresetPrototype> _presets = new();
    private readonly List<ArenaCostumePrototype> _costumes = new();
    private readonly Dictionary<ICommonSession, ArenaLoadoutEui> _activeEuis = new();

    private readonly Dictionary<NetUserId, int> _killCurrency = new();
    private readonly Dictionary<NetUserId, HashSet<string>> _ownedCostumes = new();
    private readonly Dictionary<NetUserId, List<string>> _equippedCostumes = new();
    private readonly Dictionary<NetUserId, ArenaPlayerRecord> _records = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<ArenaJoinEvent>(OnJoin);
        SubscribeNetworkEvent<ArenaLeaveEvent>(OnLeave);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PrisonerRegisteredEvent>(OnPrisonerRegistered);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void RefreshPresets()
    {
        _presets.Clear();
        foreach (var p in _protos.EnumeratePrototypes<ArenaLoadoutPresetPrototype>())
            _presets.Add(p);

        _costumes.Clear();
        foreach (var c in _protos.EnumeratePrototypes<ArenaCostumePrototype>())
            _costumes.Add(c);
    }

    private void OnJoin(ArenaJoinEvent msg, EntitySessionEventArgs args)
    {
        var who = (ICommonSession)args.SenderSession;

        if (!CanJoinArena(who))
        {
            if (_prison.IsUserPrisoner(who.UserId))
                _chat.DispatchServerMessage(who, Loc.GetString("prison-arena-blocked"));
            return;
        }

        if (who.AttachedEntity is not { Valid: true } ghost || !HasComp<GhostComponent>(ghost))
            return;

        if (_activeEuis.ContainsKey(who))
            return;

        if (_presets.Count == 0)
            RefreshPresets();

        var eui = new ArenaLoadoutEui(this, who, ghost);
        _eui.OpenEui(eui, who);
        _activeEuis[who] = eui;
    }

    private void OnPrisonerRegistered(ref PrisonerRegisteredEvent ev)
    {
        if (_activeEuis.TryGetValue(ev.Session, out var eui) && !eui.IsShutDown)
            eui.Close();

        if (ev.Session.AttachedEntity is { Valid: true } body &&
            TryComp<ArenaPlayerComponent>(body, out var arenaPlayer) &&
            _roster.Contains(GetNetEntity(body)))
        {
            RestorePlayer(body, arenaPlayer);
        }
    }

    private void OnLeave(ArenaLeaveEvent msg, EntitySessionEventArgs args)
    {
        var who = (ICommonSession)args.SenderSession;
        if (who.AttachedEntity is not { Valid: true } body ||
            !TryComp<ArenaPlayerComponent>(body, out var arenaPlayer) ||
            !_roster.Contains(GetNetEntity(body)))
            return;

        RestorePlayer(body, arenaPlayer);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!TryComp<ArenaPlayerComponent>(ev.Target, out var arenaPlayer) ||
            !_roster.Contains(GetNetEntity(ev.Target)))
            return;

        switch (ev.NewMobState)
        {
            case MobState.Critical:
                // Валюта и фраг начисляются за уход в критическое состояние, а не за добивание.
                AwardKill(ev.Origin);
                break;

            case MobState.Dead:
                RecordDeath(ev.Target);
                RestorePlayer(ev.Target, arenaPlayer);
                break;
        }
    }

    /// <summary>
    /// Учитывает смерть участника арены в статистике раунда.
    /// </summary>
    private void RecordDeath(EntityUid victim)
    {
        if (!_minds.TryGetMind(victim, out _, out var mind) || mind.UserId is not { } userId)
            return;

        var record = GetRecord(userId);
        record.Deaths++;
        if (string.IsNullOrEmpty(record.PlayerName) &&
            _player.TryGetSessionById(userId, out var session))
        {
            record.PlayerName = session.Name;
        }
    }

    /// <summary>
    /// Начисляет валюту и фраг игроку, который вывел участника арены в критическое состояние.
    /// </summary>
    private void AwardKill(EntityUid? attacker)
    {
        if (attacker is not { Valid: true } killer)
            return;

        // Награда выдаётся только за участие другого участника арены.
        if (!_roster.Contains(GetNetEntity(killer)))
            return;

        if (!_minds.TryGetMind(killer, out _, out var mind) || mind.UserId is not { } userId)
            return;

        var record = GetRecord(userId);
        record.Kills++;
        if (string.IsNullOrEmpty(record.PlayerName) &&
            _player.TryGetSessionById(userId, out var session))
        {
            record.PlayerName = session.Name;
        }

        _killCurrency.TryGetValue(userId, out var current);
        _killCurrency[userId] = current + ArenaConstants.KillCurrencyReward;
    }

    private ArenaPlayerRecord GetRecord(NetUserId userId)
    {
        if (!_records.TryGetValue(userId, out var record))
        {
            record = new ArenaPlayerRecord();
            _records[userId] = record;
        }

        return record;
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        SendManifest();
    }

    /// <summary>
    /// Собирает итоги арены за раунд (K/D) и рассылает их клиентам для вкладки в манифесте.
    /// </summary>
    private void SendManifest()
    {
        var records = new List<ArenaPlayerRecord>();
        foreach (var (userId, record) in _records)
        {
            // Дозаполняем имена на момент отправки — игрок может успеть отключиться.
            if (string.IsNullOrEmpty(record.PlayerName) &&
                _player.TryGetSessionById(userId, out var session))
            {
                record.PlayerName = session.Name;
            }

            record.KD = record.Deaths > 0 ? (double)record.Kills / record.Deaths : record.Kills;
            records.Add(record);
        }

        records.Sort((a, b) =>
        {
            var byKd = b.KD.CompareTo(a.KD);
            return byKd != 0 ? byKd : b.Kills.CompareTo(a.Kills);
        });

        RaiseNetworkEvent(new ArenaManifestEvent { Players = records });
    }

    /// <summary>
    /// Покупка костюма за валюту убийств.
    /// </summary>
    public bool TryBuyCostume(ICommonSession session, int costumeIndex)
    {
        if (costumeIndex < 0 || costumeIndex >= _costumes.Count)
            return false;

        var costume = _costumes[costumeIndex];

        var owned = GetOwned(session.UserId);
        if (owned.Contains(costume.ID))
            return false;

        _killCurrency.TryGetValue(session.UserId, out var balance);
        if (balance < costume.Price)
            return false;

        _killCurrency[session.UserId] = balance - costume.Price;
        owned.Add(costume.ID);
        return true;
    }

    /// <summary>
    /// Сохраняет выбранный набор надетой одежды для игрока.
    /// </summary>
    public void SetEquippedCostumes(ICommonSession session, List<int> costumeIndexes)
    {
        var owned = GetOwned(session.UserId);
        var equipped = GetEquipped(session.UserId);

        equipped.Clear();
        foreach (var index in costumeIndexes)
        {
            if (index < 0 || index >= _costumes.Count)
                continue;

            var costume = _costumes[index];
            if (owned.Contains(costume.ID))
                equipped.Add(costume.ID);
        }
    }

    private HashSet<string> GetOwned(NetUserId userId)
    {
        if (!_ownedCostumes.TryGetValue(userId, out var owned))
        {
            owned = new HashSet<string>();
            _ownedCostumes[userId] = owned;
        }

        return owned;
    }

    private List<string> GetEquipped(NetUserId userId)
    {
        if (!_equippedCostumes.TryGetValue(userId, out var equipped))
        {
            equipped = new List<string>();
            _equippedCostumes[userId] = equipped;
        }

        return equipped;
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        if (_activeEuis.TryGetValue(ev.Player, out var eui) && eui.SourceGhost == ev.Entity && !eui.IsShutDown)
            eui.Close();

        if (!TryComp<ArenaPlayerComponent>(ev.Entity, out var arenaPlayer) ||
            !_roster.Contains(GetNetEntity(ev.Entity)))
            return;

        // Player disconnected — full restore to preserve mind state
        if (ev.Player.Status == SessionStatus.Disconnected)
        {
            RestorePlayer(ev.Entity, arenaPlayer);
            return;
        }

        // Visiting another entity (for example via aghost) is temporary. Keep the arena body for the return.
        if (_minds.TryGetMind(ev.Entity, out _, out var temporaryMind) &&
            temporaryMind.VisitingEntity != null)
        {
            return;
        }

        // Player re-attached elsewhere (role change, admin takeover, etc.) — just clean up the arena body
        _roster.Remove(GetNetEntity(ev.Entity));
        QueueDel(ev.Entity);
    }

    public void OnLoadoutEuiClosed(ICommonSession session, ArenaLoadoutEui eui)
    {
        if (_activeEuis.TryGetValue(session, out var current) && ReferenceEquals(current, eui))
            _activeEuis.Remove(session);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        var openEuis = new List<ArenaLoadoutEui>(_activeEuis.Values);
        foreach (var eui in openEuis)
        {
            if (!eui.IsShutDown)
                eui.Close();
        }

        var query = EntityQueryEnumerator<ArenaPlayerComponent>();
        while (query.MoveNext(out var uid, out var arenaPlayer))
        {
            if (Exists(arenaPlayer.OriginalMind))
                QueueDel(arenaPlayer.OriginalMind);

            QueueDel(uid);
        }

        _activeEuis.Clear();
        _roster.Clear();
        _arenaMap = null;
        _killCurrency.Clear();
        _ownedCostumes.Clear();
        _equippedCostumes.Clear();
        _records.Clear();
    }

    public ArenaLoadoutEuiState GetLoadoutState(ICommonSession session)
    {
        if (_presets.Count == 0)
            RefreshPresets();

        var options = new List<ArenaLoadoutOption>();
        for (var i = 0; i < _presets.Count; i++)
        {
            var p = _presets[i];
            options.Add(new ArenaLoadoutOption
            {
                Index = i,
                Name = p.NameLoc,
                Description = p.DescLoc,
                Category = p.Category,
                SpritePrototype = p.IconPrototype,
            });
        }

        var costumes = new List<ArenaCostumeOption>();
        for (var i = 0; i < _costumes.Count; i++)
        {
            var c = _costumes[i];
            costumes.Add(new ArenaCostumeOption
            {
                Index = i,
                Id = c.ID,
                Name = c.NameLoc,
                Description = c.DescLoc,
                Category = c.Category,
                ItemPrototype = c.Item,
                Slot = c.Slot,
                Price = c.Price,
            });
        }

        _killCurrency.TryGetValue(session.UserId, out var balance);

        var owned = GetOwned(session.UserId);
        var ownedIndexes = new HashSet<int>();
        for (var i = 0; i < _costumes.Count; i++)
        {
            if (owned.Contains(_costumes[i].ID))
                ownedIndexes.Add(i);
        }

        var equipped = GetEquipped(session.UserId);
        var equippedIndexes = new List<int>();
        for (var i = 0; i < _costumes.Count; i++)
        {
            if (equipped.Contains(_costumes[i].ID))
                equippedIndexes.Add(i);
        }

        return new ArenaLoadoutEuiState(options, costumes, balance, ownedIndexes, equippedIndexes);
    }

    public bool SpawnPlayer(ArenaLoadoutEui eui, ICommonSession who, EntityUid sourceGhost, int kitIdx)
    {
        if (!CanJoinArena(who))
        {
            if (_prison.IsUserPrisoner(who.UserId))
            {
                _chat.DispatchServerMessage(who, Loc.GetString("prison-arena-blocked"));
                if (!eui.IsShutDown)
                    eui.Close();
            }

            return false;
        }

        if (!_activeEuis.TryGetValue(who, out var currentEui) ||
            !ReferenceEquals(currentEui, eui) ||
            who.AttachedEntity != sourceGhost ||
            !TryComp<GhostComponent>(sourceGhost, out var ghost))
            return false;

        if (!_minds.TryGetMind(who, out var originalMindId, out var originalMind))
            return false;

        EnsureMap();

        if (_arenaMap is not { } map)
            return false;

        // Clean up old dead bodies from previous lives
        SweepArenaBodies();

        if (_presets.Count == 0)
            RefreshPresets();

        var sites = new List<EntityCoordinates>();
        var cursor = AllEntityQuery<ArenaSpawnPointComponent, TransformComponent>();
        while (cursor.MoveNext(out _, out _, out var where))
        {
            if (where.MapID == Transform(map).MapID)
                sites.Add(where.Coordinates);
        }

        var spot = sites.Count > 0
            ? _luck.Pick(sites)
            : new EntityCoordinates(map, System.Numerics.Vector2.Zero);

        var profile = _prefs.GetPreferences(who.UserId).SelectedCharacter as HumanoidCharacterProfile;
        string speciesId = profile?.Species ?? SharedHumanoidAppearanceSystem.DefaultSpecies;

        // Блеклист арены: IPC и Vox на арене всегда спавнятся людьми.
        if (ArenaConstants.SpeciesBlacklist.Contains(speciesId))
        {
            speciesId = SharedHumanoidAppearanceSystem.DefaultSpecies;
            if (profile != null)
                profile = profile.WithSpecies(speciesId);
        }

        var species = _protos.Index<SpeciesPrototype>(speciesId);
        var fresh = Spawn(species.Prototype, spot);

        if (profile != null)
            _humanoid.LoadProfile(fresh, profile);

        _meta.SetEntityName(fresh, who.Name);
        GetRecord(who.UserId).PlayerName = who.Name;

        if (_presets.Count > 0)
        {
            var idx = Math.Clamp(kitIdx, 0, _presets.Count - 1);
            _stationSpawning.EquipStartingGear(fresh, _presets[idx], raiseEvent: false);
        }

        EquipCostumes(fresh, who.UserId);

        var arenaPlayer = EnsureComp<ArenaPlayerComponent>(fresh);
        arenaPlayer.OriginalMind = originalMindId;
        arenaPlayer.OriginalGhost = sourceGhost;
        arenaPlayer.CanReturnToBody = ghost.CanReturnToBody;
        EnsureComp<AntagImmuneComponent>(fresh);

        // The disposable arena body must never inherit the round mind's roles or objectives.
        _minds.SetUserId(originalMindId, null, originalMind);
        _minds.TransferTo(originalMindId, null, createGhost: false, mind: originalMind);
        var temporaryMind = _minds.CreateMind(who.UserId, who.Name);
        EnsureComp<ArenaMindComponent>(temporaryMind); // Never include the disposable arena mind in round data.
        _minds.TransferTo(temporaryMind, fresh, mind: temporaryMind.Comp);
        QueueDel(sourceGhost);
        _roles.MindAddJobRole(temporaryMind, silent: true, jobPrototype: "ArenaWarrior");

        _roster.Add(GetNetEntity(fresh));
        return true;
    }

    private void RestorePlayer(EntityUid body, ArenaPlayerComponent arenaPlayer)
    {
        _roster.Remove(GetNetEntity(body));

        if (!_minds.TryGetMind(body, out var temporaryMindId, out var temporaryMind))
        {
            QueueDel(body);
            return;
        }

        var userId = temporaryMind.UserId;

        if (temporaryMind.VisitingEntity != null)
            _minds.UnVisit(temporaryMindId, temporaryMind);

        if (userId == null || !TryComp<MindComponent>(arenaPlayer.OriginalMind, out var originalMind))
        {
            if (userId != null)
                _ghosts.SpawnGhost((temporaryMindId, temporaryMind), body, false);
            else
            {
                _minds.TransferTo(temporaryMindId, null, createGhost: false, mind: temporaryMind);
                QueueDel(temporaryMindId);
            }

            QueueDel(body);
            return;
        }

        _minds.SetUserId(temporaryMindId, null, temporaryMind);
        _minds.TransferTo(temporaryMindId, null, createGhost: false, mind: temporaryMind);

        // The source ghost was queued for deletion when the temporary mind took over.
        if (originalMind.CurrentEntity == arenaPlayer.OriginalGhost)
        {
            if (originalMind.VisitingEntity == arenaPlayer.OriginalGhost)
                _minds.UnVisit(arenaPlayer.OriginalMind, originalMind);
            else if (originalMind.OwnedEntity == arenaPlayer.OriginalGhost)
                _minds.TransferTo(arenaPlayer.OriginalMind, null, createGhost: false, mind: originalMind);
        }

        _minds.SetUserId(arenaPlayer.OriginalMind, userId.Value, originalMind);
        RestoreGhost(body, arenaPlayer, originalMind);

        QueueDel(temporaryMindId);
        QueueDel(body);
    }

    private void RestoreGhost(EntityUid arenaBody, ArenaPlayerComponent arenaPlayer, MindComponent originalMind)
    {
        var canReturn = arenaPlayer.CanReturnToBody &&
            originalMind.OwnedEntity is { } originalBody &&
            Exists(originalBody) &&
            !TerminatingOrDeleted(originalBody) &&
            !HasComp<GhostComponent>(originalBody);

        if (originalMind.CurrentEntity is { } current && TryComp<GhostComponent>(current, out var currentGhost))
        {
            _ghosts.SetCanReturnToBody((current, currentGhost), canReturn);
            return;
        }

        if (canReturn && originalMind.OwnedEntity is { } returnBody)
            _ghosts.SpawnGhost((arenaPlayer.OriginalMind, originalMind), returnBody, true);
        else
            _ghosts.SpawnGhost((arenaPlayer.OriginalMind, originalMind), arenaBody, false);
    }

    /// <summary>
    /// Надевает купленные костюмы на игрока арены, поверх экипировки пресета.
    /// </summary>
    private void EquipCostumes(EntityUid body, NetUserId userId)
    {
        var equipped = GetEquipped(userId);
        if (equipped.Count == 0)
            return;

        foreach (var costumeId in equipped)
        {
            ArenaCostumePrototype? costume = null;
            foreach (var c in _costumes)
            {
                if (c.ID == costumeId)
                {
                    costume = c;
                    break;
                }
            }

            if (costume == null)
                continue;

            if (!_protos.TryIndex<EntityPrototype>(costume.Item, out _))
                continue;

            // Освобождаем слот от штатного снаряжения пресета, чтобы костюм наделся вместо него.
            var item = Spawn(costume.Item, Transform(body).Coordinates);

            // Предметы в слотах, зависящих от заменяемого (карманы комбинезона, suitstorage и т.п.),
            // при снятии старой вещи выпадают на пол. Снимаем их без выброса на землю и
            // вернём в те же слоты после надевания нового костюма.
            var dependent = new List<(string Slot, EntityUid Item)>();
            if (_inventory.TryGetSlotEntity(body, costume.Slot, out var existing))
            {
                if (_inventory.TryGetSlots(body, out var slots))
                {
                    foreach (var slotDef in slots)
                    {
                        if (slotDef.DependsOn != costume.Slot)
                            continue;
                        if (_inventory.TryGetSlotEntity(body, slotDef.Name, out var depItem))
                            dependent.Add((slotDef.Name, depItem.Value));
                    }
                }

                foreach (var (slot, _) in dependent)
                {
                    if (_inventory.TryGetSlotContainer(body, slot, out var depContainer, out _) &&
                        depContainer.ContainedEntity is { } depUid)
                        _container.Remove(depUid, depContainer, reparent: false, force: true);
                }

                // Переносим содержимое карманов старой вещи в новую, чтобы предметы лоадаута не падали на пол.
                MoveStorageContents(existing.Value, item);

                _inventory.TryUnequip(body, costume.Slot, silent: true, force: true);
                QueueDel(existing);
            }

            var equippedOk = _inventory.TryEquip(body, item, costume.Slot, silent: true, force: true);
            if (!equippedOk)
                QueueDel(item);

            // Надеваем сохранённые предметы обратно в зависящие слоты (карманы и т.п.).
            foreach (var (slot, depItem) in dependent)
            {
                if (_inventory.TryGetSlotContainer(body, slot, out var depContainer, out _))
                    _container.Insert(depItem, depContainer, force: true);
            }

            if (!equippedOk)
                continue;

            // На жилеты автоматически применяются резисты уровня ClothingOuterArmorBasic.
            if (costume.Category == "vest")
                ApplyBasicArmor(item);
        }
    }

    /// <summary>
    /// Применяет к предмету резисты базовой брони (ClothingOuterArmorBasic: Blunt/Slash/Piercing/Heat 0.7).
    /// </summary>
    private void ApplyBasicArmor(EntityUid item)
    {
        _armor.SetModifiers(item, new DamageModifierSet
        {
            Coefficients = new Dictionary<string, float>
            {
                ["Blunt"] = 0.7f,
                ["Slash"] = 0.7f,
                ["Piercing"] = 0.7f,
                ["Heat"] = 0.7f,
            },
        }, EnsureComp<ArmorComponent>(item));
    }

    /// <summary>
    /// Переносит содержимое хранилища (карманов) старого предмета в новый, чтобы при замене одежды
    /// предметы из лоадаута не падали на пол.
    /// </summary>
    private void MoveStorageContents(EntityUid oldItem, EntityUid newItem)
    {
        if (!TryComp<StorageComponent>(oldItem, out var oldStorage) ||
            oldStorage.Container == null ||
            !TryComp<StorageComponent>(newItem, out var newStorage))
            return;

        foreach (var content in oldStorage.Container.ContainedEntities.ToArray())
        {
            _storage.Insert(newItem, content, out _, playSound: false, storageComp: newStorage);
        }
    }

    private void EnsureMap()
    {
        if (_arenaMap != null && Exists(_arenaMap.Value))
            return;

        var opts = Robust.Shared.EntitySerialization.DeserializationOptions.Default with { InitializeMaps = true };

        if (_loader.TryLoadMap(new ResPath(ArenaMapFile), out var entry, out _, opts))
        {
            _arenaMap = entry.Value.Owner;
            Log.Info($"Arena loaded: {ArenaMapFile}");
            return;
        }

        Log.Info($"No arena map at {ArenaMapFile}, building procedural arena");
        var mapUid = _maps.CreateMap(out _);
        _arenaMap = mapUid;

        var (platform, gridComp) = _mapManager.CreateGridEntity(mapUid);
        var tile = new Tile(_tiles["FloorSteel"].TileId);
        var tileList = new List<(Vector2i, Tile)>();

        for (var x = -8; x <= 8; x++)
        {
            for (var y = -8; y <= 8; y++)
            {
                tileList.Add((new Vector2i(x, y), tile));
            }
        }

        _maps.SetTiles(platform, gridComp, tileList);

        var spawnPositions = new[] { (-3, 0), (3, 0), (0, -3), (0, 3) };

        foreach (var (ox, oy) in spawnPositions)
        {
            var spot = new EntityCoordinates(platform, ox, oy);
            var ent = Spawn(null, spot);
            AddComp<ArenaSpawnPointComponent>(ent);
            _meta.SetEntityName(ent, "Arena Spawn");
        }

        _meta.SetEntityName(mapUid, "Arena");
        _meta.SetEntityName(platform, "Arena Platform");
    }

    private void SweepArenaBodies()
    {
        if (_arenaMap is not { } map || !Exists(map))
            return;

        var mid = Transform(map).MapID;

        var bodyQuery = EntityQueryEnumerator<ArenaPlayerComponent, TransformComponent>();
        while (bodyQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == mid &&
                !_roster.Contains(GetNetEntity(uid)) &&
                !_minds.TryGetMind(uid, out _, out _))
            {
                QueueDel(uid);
            }
        }

        var ghostQuery = EntityQueryEnumerator<GhostComponent, TransformComponent>();
        while (ghostQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == mid &&
                !_minds.TryGetMind(uid, out _, out _))
            {
                QueueDel(uid);
            }
        }
    }

    private void ZapArena()
    {
        if (_arenaMap is not { } map || !Exists(map))
            return;

        var mid = Transform(map).MapID;
        var graveyard = new List<EntityUid>();

        var walker = AllEntityQuery<TransformComponent>();
        while (walker.MoveNext(out var thing, out var pose))
        {
            if (!pose.ParentUid.IsValid() || pose.MapID != mid)
                continue;

            if (HasComp<MapGridComponent>(thing))
                continue;

            if (HasComp<ActorComponent>(thing) ||
                _minds.TryGetMind(thing, out _, out _))
            {
                continue;
            }

            if (HasComp<BodyPartComponent>(thing))
                continue;

            if (!HasComp<MapGridComponent>(pose.ParentUid) && pose.ParentUid != map)
                continue;

            if (!pose.Anchored || HasComp<PuddleComponent>(thing))
                graveyard.Add(thing);
        }

        foreach (var cadaver in graveyard)
            QueueDel(cadaver);
    }

    public override void Update(float frameTime)
    {
        _cleanTick += frameTime;
        if (_cleanTick < 60f)
            return;

        _cleanTick = 0f;
        ZapArena();
    }

    private float _cleanTick;
}
