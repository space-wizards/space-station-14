using Content.Server.Communications;
using Content.Server.GameTicking.Rules.Components;
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
using Content.Shared.Store.Components;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Contains the non-core logic for SSS.
/// </summary>
public sealed partial class SuspicionRuleSystem : GameRuleSystem<SuspicionRuleComponent>
{
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
}
