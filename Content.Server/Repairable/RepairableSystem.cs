using Content.Server.Administration.Logs;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Repairable
{
    public sealed class RepairableSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        }

        public async void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            // Only try repair the target if it is damaged
            if (!EntityManager.TryGetComponent(component.Owner, out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            // Can the tool actually repair this, does it have enough fuel?
            if (!await _toolSystem.UseTool(args.Used, args.User, uid, component.FuelCost, component.DoAfterDelay, component.QualityNeeded))
                return;

            if (component.Damage != null)
            {
                var damageChanged = _damageableSystem.TryChangeDamage(uid, component.Damage, true, false);
                _logSystem.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} by {damageChanged?.Total}");
            }
            else
            {
                // Repair all damage
                _damageableSystem.SetAllDamage(damageable, 0);
                _logSystem.Add(LogType.Healed, $"{ToPrettyString(args.User):user} repaired {ToPrettyString(uid):target} back to full health");
            }


            component.Owner.PopupMessage(args.User,
                Loc.GetString("comp-repairable-repair",
                    ("target", component.Owner),
                    ("tool", args.Used)));

            args.Handled = true;
        }
    }
}
