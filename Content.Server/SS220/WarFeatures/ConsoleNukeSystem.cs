using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Server.Player;
using Content.Server.Hands.Systems;
using Content.Shared.Stacks;
using Content.Server.Stack;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Events;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.ConsoleNuke;
using Content.Shared.NukeOperative;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.ConsoleNuke
{
    public sealed class ConsoleNukeSystem : EntitySystem
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPlayerManager _playerSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ConsoleNukeComponent, CommunicationConsoleUsed>(OnAnnounceMessage);
            SubscribeLocalEvent<ConsoleFTLAttemptEvent>(OnConsoleFTLAttempt);
        }

        private void OnConsoleFTLAttempt(ref ConsoleFTLAttemptEvent ev)
        {
            var nukeRule = GetNukeOpsRuleFromGrid(ev.Shuttle);
            if (nukeRule is null)
                return; //Well whatever its probably ebent then, allow.

            if (_entityManager.TryGetComponent<ConsoleNukeComponent>(ev.Shuttle, out _))
            {
                var curTime = GetTime();
                if (nukeRule.WhenAbleToMove > curTime)
                {
                    ev.Reason = Loc.GetString("nuke-console-shuttle", ("time", nukeRule.WhenAbleToMove - curTime));
                    ev.Cancelled = true;
                }
            }
        }

        private int GetTime()
        {
            return (int) _gameTicker.RoundDuration().TotalMinutes;
        }

        private bool TryGetParentGrid(EntityUid entity, [NotNullWhen(true)] out EntityUid? gridUid)
        {
            if (!TryComp<TransformComponent>(entity, out var xform))
            {
                gridUid = null;
                return false;
            }

            gridUid = xform.ParentUid;
            return true;
        }

        private NukeopsRuleComponent? GetNukeOpsRuleFromGrid(EntityUid gridId)
        {
            var query = EntityQueryEnumerator<NukeopsRuleComponent, GameRuleComponent>();
            while (query.MoveNext(out var ruleEnt, out var nukeops, out var gameRule))
            {
                if (gridId == nukeops.NukieShuttle || gridId == nukeops.NukieOutpost)
                {
                    return nukeops;
                }
            }

            return null;
        }

        private NukeopsRuleComponent? GetAssociatedNukeopsRuleComponent(EntityUid consoleUid)
        {
            if (!TryGetParentGrid(consoleUid, out var grid))
                return null;

            return GetNukeOpsRuleFromGrid(grid.Value);
        }

        private bool TryGiveTcTo(EntityUid player)
        {
            var tc = _entityManager.CreateEntityUninitialized("Telecrystal");

            if (_entityManager.TryGetComponent<StackComponent>(tc, out var component))
            {
                var stackSystem = _entitySystemManager.GetEntitySystem<StackSystem>();

                var countTC = _entityManager.HasComponent<LoneNukeOperativeComponent>(player)
                    ? _cfg.GetCVar<int>("nuke.loneoperative_tc")
                    : _cfg.GetCVar<int>("nuke.operative_tc");

                // If player count is less than 40, TC bonus downscales with playercount linearly down to 25% min.
                countTC = (int) (countTC * float.Clamp(_playerSystem.PlayerCount / 40.0f, 0.25f, 1f));

                stackSystem.SetCount(tc, countTC, component);

                var handSystem = _entitySystemManager.GetEntitySystem<HandsSystem>();

                _entityManager.InitializeAndStartEntity(tc);
                handSystem.TryForcePickupAnyHand(player, tc);

                _adminLogger.Add(LogType.WarReceiveTC, LogImpact.Medium, $"{ToPrettyString(player):player} has received {countTC} TC for declaration of war.");

                return true;
            }

            _entityManager.DeleteEntity(tc); // We should delete entity if something gone wrong
            return false;
        }

        private void OnAnnounceMessage(EntityUid uid, ConsoleNukeComponent comp,
            CommunicationConsoleUsed message)
        {
            var nukeRule = GetAssociatedNukeopsRuleComponent(uid);
            if (nukeRule is null)
                return;

            if (nukeRule.IsWarDeclated)
                return;

            if (GetTime() <= nukeRule.WhenAbleToMove)
            {
                if (message.Session.AttachedEntity is { Valid: true } player)
                {
                    if (!TryGiveTcTo(player))
                        return; //no scamming
                }

                nukeRule.WhenAbleToMove += _cfg.GetCVar<int>("nuke.operative_additionaltime");
                nukeRule.IsWarDeclated = true;
            }
        }
    }
}
