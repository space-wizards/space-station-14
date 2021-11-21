using Content.Server.Damage.Components;
using Content.Server.Tools.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Damage.Systems
{
    public class DamageOnToolInteractSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

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
                && args.Used.TryGetComponent<WelderComponent>(out var welder)
                && welder.Lit)
            {
                _damageableSystem.TryChangeDamage(args.Target.Uid, weldingDamage);
                args.Handled = true;
                return;
            }

            if (component.DefaultDamage is {} damage
                && args.Used.TryGetComponent<ToolComponent>(out var tool)
                && tool.Qualities.ContainsAny(component.Tools))
            {
                _damageableSystem.TryChangeDamage(args.Target.Uid, damage);
                args.Handled = true;
                return;
            }
        }
    }
}
