using Content.Server.Tools.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Repairable
{
    public class ReairableSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<RepairableComponent, InteractUsingEvent>(Repair);
        }

        public async void Repair(EntityUid uid, RepairableComponent component, InteractUsingEvent args)
        {
            // Only repair if you are using a lit welder
            if (!args.Used.TryGetComponent(out WelderComponent? welder) || !welder.WelderLit)
                return;

            // Only try repair the target if it is damaged
            if (!component.Owner.TryGetComponent(out DamageableComponent? damageable) || damageable.TotalDamage == 0)
                return;

            // Can the welder actually repair this, does it have enough fuel?
            if (!await welder.UseTool(args.User, component.Owner, component.DoAfterDelay, ToolQuality.Welding, component.FuelCost))
                return;

            // Repair all damage
            _damageableSystem.SetAllDamage(damageable, 0);

            component.Owner.PopupMessage(args.User,
                Loc.GetString("comp-repairable-repair",
                    ("target", component.Owner),
                    ("welder", args.Used)));

            args.Handled = true;
        }
    }
}
