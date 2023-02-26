using Content.Server.Administration.Logs;
using Content.Server.Tools;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;

namespace Content.Server.Repairable
{
    public sealed class RepairableSystem : EntitySystem
    {
        [Dependency] private readonly SharedToolSystem _toolSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        }

        public async void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            // Only try repair the target if it is damaged
            if (!EntityManager.TryGetComponent(component.Owner, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            float delay = component.DoAfterDelay;

            // Add a penalty to how long it takes if the user is repairing itself
            if (args.User == args.Target)
                delay *= component.SelfRepairPenalty;

            var toolEvData = new ToolEventData(null);

            // Can the tool actually repair this, does it have enough fuel?
            if (!_toolSystem.UseTool(args.Used, args.User, uid, delay, component.QualityNeeded, toolEvData, component.FuelCost))
                return;

            if (component.Damage != null)
            {
                var damageChanged = _damageableSystem.TryChangeDamage(uid, component.Damage, true, false, origin: args.User);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.Total}");
            }
            else
            {
                // Repair all damage
                _damageableSystem.SetAllDamage(damageable, 0);
                _adminLogger.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} back to full health");
            }


            component.Owner.PopupMessage(args.User,
                Loc.GetString("comp-repairable-repair",
                    ("target", component.Owner),
                    ("tool", args.Used)));

            args.Handled = true;
        }
    }
}
