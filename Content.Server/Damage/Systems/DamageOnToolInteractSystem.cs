using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using ItemToggleComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleComponent;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageOnToolInteractSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DamageOnToolInteractComponent, InteractUsingEvent>(OnInteracted);
        }

        private void OnInteracted(EntityUid uid, DamageOnToolInteractComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<ItemToggleComponent>(args.Used, out var itemToggle))
                return;

            if (component.WeldingDamage is {} weldingDamage
            && EntityManager.TryGetComponent(args.Used, out WelderComponent? welder)
            && itemToggle.Activated
            && !welder.TankSafe)
            {
                var dmg = _damageableSystem.TryChangeDamage(args.Target, weldingDamage, origin: args.User);

                if (dmg != null)
                    _adminLogger.Add(LogType.Damaged,
                        $"{ToPrettyString(args.User):user} used {ToPrettyString(args.Used):used} as a welder to deal {dmg.GetTotal():damage} damage to {ToPrettyString(args.Target):target}");

                args.Handled = true;
            }
            else if (component.DefaultDamage is {} damage
                && EntityManager.TryGetComponent(args.Used, out ToolComponent? tool)
                && tool.Qualities.ContainsAny(component.Tools))
            {
                var dmg = _damageableSystem.TryChangeDamage(args.Target, damage, origin: args.User);

                if (dmg != null)
                    _adminLogger.Add(LogType.Damaged,
                        $"{ToPrettyString(args.User):user} used {ToPrettyString(args.Used):used} as a tool to deal {dmg.GetTotal():damage} damage to {ToPrettyString(args.Target):target}");

                args.Handled = true;
            }
        }
    }
}
