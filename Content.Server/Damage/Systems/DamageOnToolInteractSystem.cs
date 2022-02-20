using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Server.Tools.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageOnToolInteractSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DamageOnToolInteractComponent, InteractUsingEvent>(OnInteracted);
        }

        private void OnInteracted(EntityUid uid, DamageOnToolInteractComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (component.WeldingDamage is {} weldingDamage
                && EntityManager.TryGetComponent<WelderComponent?>(args.Used, out var welder)
                && welder.Lit
                && !welder.TankSafe)
            {
                var dmg = _damageableSystem.TryChangeDamage(args.Target, weldingDamage);

                if (dmg != null)
                    _logSystem.Add(LogType.Damaged,
                        $"{ToPrettyString(args.User):user} used {ToPrettyString(args.Used):used} as a welder to deal {dmg.Total:damage} damage to {ToPrettyString(args.Target):target}");

                args.Handled = true;
            }
            else if (component.DefaultDamage is {} damage
                && EntityManager.TryGetComponent<ToolComponent?>(args.Used, out var tool)
                && tool.Qualities.ContainsAny(component.Tools))
            {
                var dmg = _damageableSystem.TryChangeDamage(args.Target, damage);

                if (dmg != null)
                    _logSystem.Add(LogType.Damaged,
                        $"{ToPrettyString(args.User):user} used {ToPrettyString(args.Used):used} as a tool to deal {dmg.Total:damage} damage to {ToPrettyString(args.Target):target}");

                args.Handled = true;
            }
        }
    }
}
