using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent
    {
        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out IDamageableComponent damageableComponent))
            {
                damageableComponent.HealthChangedEvent += OnDamage;
            }
        }

        private void OnDamage(HealthChangedEventArgs eventArgs)
        {
            int current = eventArgs.Damageable.TotalDamage;
            int max = eventArgs.Damageable.Thresholds[DamageState.Dead];
            UpdateVisuals(current, max);
        }

        private void UpdateVisuals(int currentDamage, int maxDamage)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(WindowVisuals.Damage, (float) currentDamage / maxDamage);
            }
        }
    }
}
