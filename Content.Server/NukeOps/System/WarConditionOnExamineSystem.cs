using Content.Shared.NukeOps;
using JetBrains.Annotations;
using Content.Server.GameTicking.Rules;
using Content.Shared.Examine;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Timing;
using Content.Server.NukeOps.Components;

namespace Content.Server.NukeOps.System
{
    /// <summary>
    /// Shows information about war conditions on examine
    /// </summary>
    [UsedImplicitly]
    public sealed class WarConditionOnExamineSystem : EntitySystem
    {
        [Dependency] private readonly NukeopsRuleSystem _nukeopsRuleSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WarConditionOnExamineComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, WarConditionOnExamineComponent component, ExaminedEvent args)
        {
            // This component can be applied to something that must be powered
            if (!_nukeopsRuleSystem.RuleAdded || !_powerReceiverSystem.IsPowered(uid))
            {
                return;
            }

            var condition = _nukeopsRuleSystem.GetWarCondition();
            switch (condition)
            {
                case WarConditionStatus.NO_WAR_UNKNOWN:
                    args.PushMarkup($"{Loc.GetString("war-ops-examine-status-unable")}\n{Loc.GetString("war-ops-examine-conditions-unknown")}");
                    break;
                case WarConditionStatus.NO_WAR_SHUTTLE_DEPARTED:
                    args.PushMarkup($"{Loc.GetString("war-ops-examine-status-unable")}\n{Loc.GetString("war-ops-examine-conditions-left-outpost")}");
                    break;
                case WarConditionStatus.NO_WAR_TIMEOUT:
                    args.PushMarkup($"{Loc.GetString("war-ops-examine-status-unable")}\n{Loc.GetString("war-ops-examine-conditions-time-out")}");
                    break;
                case WarConditionStatus.NO_WAR_SMALL_CREW:
                    args.PushMarkup($"{Loc.GetString("war-ops-examine-status-unable")}\n{Loc.GetString("war-ops-examine-conditions-small-crew", ("min_size", _nukeopsRuleSystem.Config.WarDeclarationMinOpsSize))}");
                    break;
                case WarConditionStatus.YES_WAR:
                    var gameruleTime = _gameTiming.CurTime.Subtract(_nukeopsRuleSystem.GameruleStartTime);
                    var timeLeft = _nukeopsRuleSystem.Config.WarDeclarationTimeWindow.Subtract(gameruleTime);
                    args.PushMarkup($"{Loc.GetString("war-ops-examine-status-able")}\n{Loc.GetString("war-ops-examine-conditions-timer", ("minutes", timeLeft.Minutes), ("seconds", timeLeft.Seconds))}");
                    break;
                case WarConditionStatus.WAR_DELAY:
                    var timeAfterDeclaration = _gameTiming.CurTime.Subtract(_nukeopsRuleSystem.DeclarationTime);
                    var timeRemain = _nukeopsRuleSystem.Config.WarNukieArriveDelay.Subtract(timeAfterDeclaration);
                    args.PushMarkup($"{Loc.GetString("war-ops-examine-status-declared")}\n{Loc.GetString("war-ops-examine-conditions-delay")}\n{Loc.GetString("war-ops-examine-conditions-timer", ("minutes", timeRemain.Minutes), ("seconds", timeRemain.Seconds))}");
                    break;
                case WarConditionStatus.WAR_READY:
                    args.PushMarkup($"{Loc.GetString("war-ops-examine-status-declared")}\n{Loc.GetString("war-ops-examine-conditions-ready")}");
                    break;
            }
        }
    }
}