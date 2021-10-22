using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Repairable
{
    public class RepairableSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        }

        public async void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            // Only try repair the target if it is damaged
            if (!component.Owner.TryGetComponent(out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            // Can the tool actually repair this, does it have enough fuel?
            if (!await _toolSystem.UseTool(args.Used.Uid, args.User.Uid, uid, component.FuelCost, component.DoAfterDelay, component.QualityNeeded))
                return;

            // Repair all damage
            _damageableSystem.SetAllDamage(damageable, 0);

            component.Owner.PopupMessage(args.User,
                Loc.GetString("comp-repairable-repair",
                    ("target", component.Owner),
                    ("tool", args.Used)));

            args.Handled = true;
        }
    }
}
