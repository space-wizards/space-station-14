using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.Atmos.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Radio.Components;
using Content.Server.Roles;
using Content.Server.Temperature.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.NukeOps;
using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.Players;
using Content.Shared.Security.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SuspicionRuleSystem : GameRuleSystem<SuspicionRuleComponent>
{
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
}
