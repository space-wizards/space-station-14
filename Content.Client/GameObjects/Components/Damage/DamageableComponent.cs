using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Damage
{
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [ComponentReference(typeof(SharedDamageableComponent))]
    public class DamageableComponent : SharedDamageableComponent
    {
    }
}
