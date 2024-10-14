using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Systems;
using Content.Server.Antag;
using Content.Server.Atmos.Components;
using Content.Server.Chat.Managers;
using Content.Server.Communications;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Implants;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Pinpointer;
using Content.Server.Radio.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.Temperature.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.NukeOps;
using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.Players;
using Content.Shared.Security.Components;
using Content.Shared.Store.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking;

/// <summary>
/// Manages the "Suspicion on the Space Station" gamemode. TTT but in space station 14.
/// </summary>
public sealed class SuspicionRuleSystem : GameRuleSystem<SuspicionRuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelectionSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SubdermalImplantSystem _subdermalImplant = default!;
    [Dependency] private readonly AccessSystem _accessSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly NavMapSystem _navMapSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    private readonly SoundSpecifier _traitorStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<SuspicionRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<SuspicionPlayerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SuspicionPlayerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCall);
    }

    private void OnShuttleCall(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<SuspicionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var sus, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            ev.Cancelled = true;
        }
    }

    protected override void AppendRoundEndText(EntityUid uid,
        SuspicionRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var traitors = FindAllOfType(SuspicionRole.Traitor, false);
        var innocents = FindAllOfType(SuspicionRole.Innocent, false);
        var detectives = FindAllOfType(SuspicionRole.Detective, false);

        var traitorsOnlyAlive = FindAllOfType(SuspicionRole.Traitor);
        var innocentsOnlyAlive = FindAllOfType(SuspicionRole.Innocent);
        var detectivesOnlyAlive = FindAllOfType(SuspicionRole.Detective);

        void Append(List<EntityUid> people, ref RoundEndTextAppendEvent args)
        {
            foreach (var person in people)
            {
                var name = MetaData(person).EntityName;
                var isDead = _mobState.IsDead(person);
                if (isDead)
                    args.AddLine($"[bullet/] {name} (Dead)");
                else
                    args.AddLine($"[bullet/] {name}");
            }
        }

        var victory = innocentsOnlyAlive.Count + detectivesOnlyAlive.Count == 0 ? "Traitors" : "Innocents";
        // Traitors win if there are no innocents or detectives left.

        args.AddLine($"[bold]{victory}[/bold] have won the round.");

        args.AddLine($"[color=red][bold]Traitors[/bold][/color]: {traitors.Count}");
        Append(traitors.Select(t => t.body).ToList(), ref args);
        args.AddLine($"[color=green][bold]Innocents[/bold][/color]: {innocents.Count}");
        Append(innocents.Select(t => t.body).ToList(), ref args);
        args.AddLine($"[color=blue][bold]Detectives[/bold][/color]: {detectives.Count}");
        Append(detectives.Select(t => t.body).ToList(), ref args);
    }

    private void OnMobStateChanged(EntityUid uid, SuspicionPlayerComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical)
        {
            var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Genetic"), 90000);
            _damageableSystem.TryChangeDamage(args.Target, damageSpec);
            Log.Debug("Player is critical, applying genetic damage.");
            return;
        }

        if (args.NewMobState != MobState.Dead) // Someone died.
            return;

        var query = EntityQueryEnumerator<SuspicionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleId, out var sus, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(ruleId, gameRule))
                continue;

            if (sus.GameState != SuspicionGameState.InProgress)
                break;

            sus.EndAt += TimeSpan.FromSeconds(sus.TimeAddedPerKill);
            sus.AnnouncedTimeLeft.Clear();

            var allTraitors = FindAllOfType(SuspicionRole.Traitor);
            // Ok this is fucking horrible
            foreach (var traitor in allTraitors)
            {
                var implantedComponent = CompOrNull<ImplantedComponent>(traitor.body);
                if (implantedComponent == null)
                    continue;

                foreach (var implant in implantedComponent.ImplantContainer.ContainedEntities)
                {
                    var storeComp = CompOrNull<StoreComponent>(implant);
                    if (storeComp == null)
                        continue;

                    _storeSystem.TryAddCurrency(new Dictionary<string, FixedPoint2>()
                        {
                            { "Telecrystal", sus.AmountAddedPerKill },
                        },
                        implant,
                        storeComp
                    );
                }
            }

            var message = Loc.GetString("tc-added-sus", ("tc", sus.AmountAddedPerKill));

            var channels = new List<INetChannel>();
            foreach (var traitor in allTraitors)
            {
                var found = _playerManager.TryGetSessionByEntity(traitor.body, out var channel);
                if (found)
                    channels.Add(channel!.Channel);
            }
            _chatManager.ChatMessageToMany(ChatChannel.Server, message, message, EntityUid.Invalid, false, true, channels);

            var allInnocents = FindAllOfType(SuspicionRole.Innocent);
            var allDetectives = FindAllOfType(SuspicionRole.Detective);

            if (allInnocents.Count == 0 && allDetectives.Count == 0)
            {
                _chatManager.DispatchServerAnnouncement("The traitors have won the round.");
                _roundEndSystem.EndRound(TimeSpan.FromSeconds(sus.PostRoundDuration));
                return;
            }

            if (allTraitors.Count == 0)
            {
                _chatManager.DispatchServerAnnouncement("The innocents have won the round.");
                _roundEndSystem.EndRound(TimeSpan.FromSeconds(sus.PostRoundDuration));
                return;
            }
            break;
        }
    }

    private List<(EntityUid body, Entity<SuspicionRoleComponent> sus)> FindAllOfType(SuspicionRole role, bool filterDead = true)
    {
        var allMinds = new List<EntityUid>();
        if (filterDead)
        {
            allMinds = _mindSystem.GetAliveHumansExcept(EntityUid.Invalid);
        }
        else
        {
            var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent>();
            while (query.MoveNext(out var _, out var mc, out _))
            {
                // the player needs to have a mind and not be the excluded one
                if (mc.Mind == null)
                    continue;

                allMinds.Add(mc.Mind.Value);
            }
        }

        var result = new List<(EntityUid body, Entity<SuspicionRoleComponent>)>();
        foreach (var mind in allMinds)
        {
            if (!_roleSystem.MindHasRole<SuspicionRoleComponent>(mind, out var _, out var roleComp))
                continue;

            if (roleComp.Value.Comp.Role != role)
                continue;

            var entity = Comp<MindComponent>(mind).OwnedEntity;
            if (!entity.HasValue)
                continue;

            result.Add((entity.Value, roleComp.Value));
        }

        return result;
    }

    /// <summary>
    /// This is DispatchServerAnnouncement but markdown is not escaped (why is it escaped in the first place on a server announcment???)
    /// </summary>
    private void SendAnnouncement(string message, Color? colorOverride = null)
    {
        _chatManager.ChatMessageToAll(
            ChatChannel.Server,
            message,
            message,
            EntityUid.Invalid,
            hideChat: false,
            recordReplay: true,
            colorOverride: colorOverride);
    }

    private void OnExamine(EntityUid uid, SuspicionPlayerComponent component, ref ExaminedEvent args)
    {
        if (!TryComp<MobStateComponent>(args.Examined, out var mobState))
            return;

        if (!_mobState.IsDead(args.Examined, mobState))
            return; // Not a dead body... *yet*.

        var isInRange = args.IsInDetailsRange || component.Revealed;
        // Always show the role if it was already announced in chat.

        if (!isInRange)
        {
            args.PushText("Get closer to examine the body.", -10);
            return;
        }

        var mind = _mindSystem.GetMind(args.Examined);

        if (mind == null)
            return;

        if (!_roleSystem.MindHasRole<SuspicionRoleComponent>(mind.Value, out var _, out var role))
            return;

        if (role.Value.Comp.Role == SuspicionRole.Pending)
            return;

        args.PushMarkup(Loc.GetString(
            "suspicion-examination",
            ("ent", args.Examined),
            ("col", role.Value.Comp.Role.GetRoleColor()),
            ("role", role.Value.Comp.Role.ToString())),
            -10);

        if (!HasComp<HandsComponent>(args.Examiner))
            return;

        if (HasComp<GhostComponent>(args.Examiner))
            return; // Check for admin ghosts

        // Reveal the role in chat
        if (component.Revealed)
            return;

        component.Revealed = true;
        var trans = Comp<TransformComponent>(args.Examined);
        var loc = _transformSystem.GetMapCoordinates(trans);

        var msg = Loc.GetString("suspicion-examination-chat",
            ("finder", args.Examiner),
            ("found", args.Examined),
            ("where", _navMapSystem.GetNearestBeaconString(loc)),
            ("col", role.Value.Comp.Role.GetRoleColor()),
            ("role", role.Value.Comp.Role.ToString()));
        SendAnnouncement(
            msg
        );
    }

    private void OnGetBriefing(Entity<SuspicionRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Briefing = role.Comp.Role switch
        {
            SuspicionRole.Traitor => Loc.GetString("roles-antag-suspicion-traitor-objective"),
            SuspicionRole.Detective => Loc.GetString("roles-antag-suspicion-detective-objective"),
            SuspicionRole.Innocent => Loc.GetString("roles-antag-suspicion-innocent-objective"),
            _ => Loc.GetString("roles-antag-suspicion-pending-objective")
        };
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var allAccess = _prototypeManager
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Select(p => new ProtoId<AccessLevelPrototype>(p.ID))
            .ToArray();

        var query = EntityQueryEnumerator<SuspicionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var sus, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (sus.GameState != SuspicionGameState.Preparing)
            {
                Log.Debug("Player tried to join a game of Suspicion but the game is not in the preparing state.");
                _chatManager.DispatchServerMessage(ev.Player, "Sorry, the game has already started. You have been made an observer.");
                GameTicker.SpawnObserver(ev.Player); // Players can't join mid-round.
                ev.Handled = true;
                return;
            }

            var newMind = _mindSystem.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mindSystem.SetUserId(newMind, ev.Player.UserId);

            var mobMaybe = _stationSpawningSystem.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            var mob = mobMaybe!.Value;

            _mindSystem.TransferTo(newMind, mob);
            SetOutfitCommand.SetOutfit(mob, sus.Gear, EntityManager);
            _roleSystem.MindAddRole(newMind, "MindRoleSuspicion");

            // Rounds only last like 5 minutes, so players shouldn't need to eat or drink.
            RemComp<ThirstComponent>(mob);
            RemComp<HungerComponent>(mob);

            EnsureComp<ShowCriminalRecordIconsComponent>(mob); // Hijacking criminal records for the blue "D" symbol.

            // Because of the limited tools available to crew, we need to make sure that spacings are not lethal.
            EnsureComp<BarotraumaComponent>(mob).MaxDamage = 90;
            EnsureComp<TemperatureComponent>(mob).ColdDamageThreshold = float.MinValue;

            EnsureComp<IntrinsicRadioTransmitterComponent>(mob);
            _accessSystem.TrySetTags(mob, allAccess, EnsureComp<AccessComponent>(mob));

            EnsureComp<SuspicionPlayerComponent>(mob);

            RemComp<PerishableComponent>(mob);
            RemComp<RottingComponent>(mob); // No rotting bodies in this mode, can't revive them anyways.

            EnsureComp<UnrevivableComponent>(mob);
            EnsureComp<KillTrackerComponent>(mob);
            EnsureComp<BodyComponent>(mob).CanGib = false; // Examination is important.

            ev.Handled = true;
            break;
        }
    }

    protected override void Started(EntityUid uid, SuspicionRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.GameState = SuspicionGameState.Preparing;

        Timer.Spawn(TimeSpan.FromSeconds(component.PreparingDuration - 5), () =>  _chatManager.DispatchServerAnnouncement("The round will start in 5 seconds."));
        Timer.Spawn(TimeSpan.FromSeconds(component.PreparingDuration), () =>  StartRound(uid, component, gameRule));
        Log.Debug("Starting a game of Suspicion.");
    }

    private void StartRound(EntityUid uid, SuspicionRuleComponent component, GameRuleComponent gameRule)
    {
        component.GameState = SuspicionGameState.InProgress;
        component.EndAt = TimeSpan.FromSeconds(component.RoundDuration);

        var allPlayerData = _playerManager.GetAllPlayerData().ToList();
        var participatingPlayers = new List<(EntityUid mind, SuspicionRoleComponent comp)>();
        foreach (var sessionData in allPlayerData)
        {
            var contentData = sessionData.ContentData();
            if (contentData == null)
                continue;

            if (!contentData.Mind.HasValue)
                continue;

            if (!_roleSystem.MindHasRole<SuspicionRoleComponent>(contentData.Mind.Value, out var _, out var role))
                continue; // Player is not participating in the game.

            participatingPlayers.Add((contentData.Mind.Value, role));
        }

        if (participatingPlayers.Count == 0)
        {
            _chatManager.DispatchServerAnnouncement("The round has started but there are no players participating. Restarting", Color.Red);
            _roundEndSystem.EndRound(TimeSpan.FromSeconds(5));
            return;
        }

        foreach (var participatingPlayer in participatingPlayers)
        {
            var ent = Comp<MindComponent>(participatingPlayer.mind).OwnedEntity;
            if (ent.HasValue)
                _rejuvenate.PerformRejuvenate(ent.Value);
        }

        var traitorCount = MathHelper.Clamp((int) (participatingPlayers.Count * component.TraitorPercentage), 1, allPlayerData.Count);
        var detectiveCount = MathHelper.Clamp((int) (participatingPlayers.Count * component.DetectivePercentage), 0, allPlayerData.Count);

        RobustRandom.Shuffle(participatingPlayers); // Shuffle the list so we can just take the first N players
        RobustRandom.Shuffle(participatingPlayers);
        RobustRandom.Shuffle(participatingPlayers); // I don't trust the shuffle.
        RobustRandom.Shuffle(participatingPlayers);
        RobustRandom.Shuffle(participatingPlayers); // I really don't trust the shuffle.


        for (var i = 0; i < traitorCount; i++)
        {
            var role = participatingPlayers[i];
            role.comp.Role = SuspicionRole.Traitor;
            var ownedEntity = Comp<MindComponent>(role.mind).OwnedEntity;
            if (!ownedEntity.HasValue)
            {
                Log.Error("Player mind has no entity.");
                continue;
            }

            // Hijacking the nuke op systems to show fellow traitors. Don't have to reinvent the wheel.
            EnsureComp<NukeOperativeComponent>(ownedEntity.Value);
            EnsureComp<ShowSyndicateIconsComponent>(ownedEntity.Value);
            EnsureComp<IntrinsicRadioTransmitterComponent>(ownedEntity.Value).Channels.Add(component.TraitorRadio);

            _npcFactionSystem.AddFaction(ownedEntity.Value, component.TraitorFaction);

            _subdermalImplant.AddImplants(ownedEntity.Value, new List<string> {component.UplinkImplant}); // Why does this method only take in a list???

            _antagSelectionSystem.SendBriefing(
                ownedEntity.Value,
                Loc.GetString("traitor-briefing"),
                Color.Red,
                _traitorStartSound);
        }

        for (var i = traitorCount; i < traitorCount + detectiveCount; i++)
        {
            var role = participatingPlayers[i];
            role.comp.Role = SuspicionRole.Detective;
            var ownedEntity = Comp<MindComponent>(role.mind).OwnedEntity;
            if (!ownedEntity.HasValue)
            {
                Log.Error("Player mind has no entity.");
                continue;
            }

            EnsureComp<CriminalRecordComponent>(ownedEntity.Value).StatusIcon = "SecurityIconDischarged";

            _subdermalImplant.AddImplants(ownedEntity.Value, new List<string> {component.DetectiveImplant});

            _antagSelectionSystem.SendBriefing(
                ownedEntity.Value,
                Loc.GetString("detective-briefing"),
                Color.Blue,
                briefingSound:null);
        }

        // Anyone who isn't a traitor will get the innocent role.
        foreach (var (mind, role) in participatingPlayers)
        {
            if (role.Role != SuspicionRole.Pending)
                continue;

            role.Role = SuspicionRole.Innocent;
            var ownedEntity = Comp<MindComponent>(mind).OwnedEntity;
            if (!ownedEntity.HasValue)
                continue;

            _antagSelectionSystem.SendBriefing(
                ownedEntity.Value,
                Loc.GetString("innocent-briefing"),
                briefingColor: Color.Green,
                briefingSound:null);
        }

        _chatManager.DispatchServerAnnouncement($"The round has started. There are {traitorCount} traitors among us.");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SuspicionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var sus, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (sus.GameState != SuspicionGameState.InProgress)
                continue;

            sus.EndAt -= TimeSpan.FromSeconds(frameTime);

            var timeLeft = sus.EndAt.TotalSeconds;
            switch (timeLeft)
            {
                case <= 240 when !sus.AnnouncedTimeLeft.Contains(240):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(240);
                    break;
                case <= 180 when !sus.AnnouncedTimeLeft.Contains(180):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(180);
                    break;
                case <= 120 when !sus.AnnouncedTimeLeft.Contains(120):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(120);
                    break;
                case <= 60 when !sus.AnnouncedTimeLeft.Contains(60):
                    _chatManager.DispatchServerAnnouncement($"The round will end in {Math.Round(sus.EndAt.TotalMinutes)}:{sus.EndAt.Seconds}.");
                    sus.AnnouncedTimeLeft.Add(60);
                    break;
                case <= 30 when !sus.AnnouncedTimeLeft.Contains(30):
                    _chatManager.DispatchServerAnnouncement($"The round will end in 30 seconds.");
                    sus.AnnouncedTimeLeft.Add(30);
                    break;
                case <= 10 when !sus.AnnouncedTimeLeft.Contains(10):
                    _chatManager.DispatchServerAnnouncement($"The round will end in 10 seconds.");
                    sus.AnnouncedTimeLeft.Add(10);
                    break;
                case <= 5 when !sus.AnnouncedTimeLeft.Contains(5):
                    _chatManager.DispatchServerAnnouncement($"The round will end in 5 seconds.");
                    sus.AnnouncedTimeLeft.Add(5);
                    break;
            }

            if (sus.EndAt <= TimeSpan.Zero)
            {
                sus.GameState = SuspicionGameState.PostRound;
                _roundEndSystem.EndRound(TimeSpan.FromSeconds(sus.PostRoundDuration));
                return;
            }
        }
    }
}
