using Content.Server.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using Content.Shared.Tools;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;
using Content.Server.DoAfter;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Tools.Components;
using Content.Server.Construction.Conditions;
using Content.Shared.Tag;
using Content.Server.Chemistry.Containers.EntitySystems;


namespace Content.Server.Repairable
{
    public sealed class RepairableSystem : SharedRepairableSystem
    {
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly SharedToolSystem _toolSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
            SubscribeLocalEvent<RepairableComponent, RepairFinishedEvent>(OnRepairFinished);
        }

        private void OnRepairFinished(EntityUid uid, RepairableComponent component, RepairFinishedEvent args)
        {
            ICommonSession? session = null;

            if (args.Cancelled)
                return;

            if (!EntityManager.TryGetComponent(uid, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            if (component.Damage != null)
            {
                var damageChanged = _damageableSystem.TryChangeDamage(uid, component.Damage, true, false, origin: args.User);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.GetTotal()}");
            }           
            else
            {
                // Repair all damage
                _damageableSystem.SetAllDamage(uid, damageable, 0);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} back to full health");

                // this is to revive gnomes and call their ghost back
                //check for target for threshholds, i hardly understand WHY this works but it does so i wont touch it
                if (TryComp(uid, out MobThresholdsComponent? mobthresholds))
                {
                    if (_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) &&
                    TryComp<DamageableComponent>(uid, out var damageableComponent) &&
                   damageableComponent.TotalDamage < threshold)
                    {
                        _mobState.ChangeMobState(uid, MobState.Alive, null, uid);
                    }
                    if (_mind.TryGetMind(uid, out _, out var mind) &&
                   mind.Session is { } playerSession)
                    {
                        session = playerSession;
                        // notify them they're being revived.
                        if (mind.CurrentEntity != uid)
                        {
                            _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), session);
                        }
                    }
                }

            }
            //everything below is work in progress shitcode to try and remove glue from the fuckin bottle
            var tool = args.Used.Value;
            
              //remove glue from glue tubes to revive gnomes or to fix other items. STOP GIVING ME ERRORS YOU TOUCHY FUCKING TWAT I HAVENT EATEN TODAY BECAUSE OF YOU WORK GOD DAMNIT!
              //TODO: cry
                if (_tag.HasTag(tool, "GlueTool"))
                {

                }
            
            var str = Loc.GetString("comp-repairable-repair",
                ("target", uid),
                ("tool", args.Used!));
            _popup.PopupEntity(str, uid, args.User);
        }

        public async void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            //TODO: CHECK TO SEE IF GLUE BOTTLE IS EMPTY

            // Only try repair the target if it is damaged
            if (!TryComp<DamageableComponent>(uid, out var damageable) || damageable.TotalDamage == 0)
                return;

            float delay = component.DoAfterDelay;

            // Add a penalty to how long it takes if the user is repairing itself
            if (args.User == args.Target)
            {
                if (!component.AllowSelfRepair)
                    return;

                delay *= component.SelfRepairPenalty;
            }

            // Run the repairing doafter
            args.Handled = _toolSystem.UseTool(args.Used, args.User, uid, delay, component.QualityNeeded, new RepairFinishedEvent());
        }
    }
}
