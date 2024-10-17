using Content.Server.Communications;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost;
using Content.Server.Radiation.Components;
using Content.Server.Roles;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.Store.Components;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Contains the non-core logic for SSS.
/// </summary>
public sealed partial class SuspicionRuleSystem : GameRuleSystem<SuspicionRuleComponent>
{
    private void OnGhost(GhostSpawnedEvent ghost)
    {
        var query = EntityQueryEnumerator<SuspicionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var ruleId, out var _, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(ruleId, gameRule))
                continue;

            // Only apply the overlay to ghosts when the gamemode is active.

            EnsureComp<ShowSyndicateIconsComponent>(ghost.Ghost);
            EnsureComp<ShowCriminalRecordIconsComponent>(ghost.Ghost);
            break;
        }
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

    private void UpdateSpaceWalkDamage(ref SuspicionRuleComponent sus, float frameTime)
    {
        var query = EntityQueryEnumerator<SuspicionPlayerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.SpacewalkThreshold <= 0)
                continue;

            var coordinates = _transformSystem.GetMapCoordinates(uid);

            var entities = _entityLookupSystem.GetEntitiesInRange<SuspicionGridMarkerComponent>(coordinates,
                comp.SpacewalkThreshold,
                LookupFlags.Sundries);

            if (entities.Count > 0)
                continue;

            if (comp.LastTookSpacewalkDamage + TimeSpan.FromSeconds(1) > DateTime.Now)
                continue;

            var damage = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Toxin"), 5);
            _damageableSystem.TryChangeDamage(uid, damage);
            comp.LastTookSpacewalkDamage = DateTime.Now;
            _popupSystem.PopupEntity("You feel an outside force pressing in on you. Maybe try going back inside?",
                uid,
                uid,
                PopupType.LargeCaution);
        }
    }

    private void UpdateTimer(ref SuspicionRuleComponent sus, float frameTime)
    {
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

        if (sus.EndAt > TimeSpan.Zero)
            return;

        sus.GameState = SuspicionGameState.PostRound;
        _roundEndSystem.EndRound(TimeSpan.FromSeconds(sus.PostRoundDuration));
    }
}
