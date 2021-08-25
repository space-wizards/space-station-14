using Content.Server.Tools.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Repairable
{
    public class ReairableSystem : EntitySystem
    {
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
            RaiseLocalEvent(uid, new SetAllDamageEvent(0), false);

            component.Owner.PopupMessage(args.User,
                Loc.GetString("comp-repairable-repair",
                    ("target", component.Owner),
                    ("welder", args.Used)));

            args.Handled = true;
        }
    }
}
