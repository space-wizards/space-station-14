using Content.Shared.Damage;
using Robust.Shared.GameObjects;

namespace Content.Server.Window
{
    public class WindowSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WindowComponent, DamageChangedEvent>(UpdateVisuals);
        }

        public void UpdateVisuals(EntityUid _, WindowComponent component, DamageChangedEvent args)
        {
            component.UpdateVisuals(args.Damageable.TotalDamage);
        }
    }
}
