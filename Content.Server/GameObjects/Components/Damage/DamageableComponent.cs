using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [ComponentReference(typeof(SharedDamageableComponent))]
    public class DamageableComponent : SharedDamageableComponent
    {
        protected override void OnHealthChanged(DamageChangedEventArgs e)
        {
            base.OnHealthChanged(e);

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DamageVisualizerData.TotalDamage, TotalDamage);
            }
        }
    }
}
