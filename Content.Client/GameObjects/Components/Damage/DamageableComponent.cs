using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Damage
{
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    public class DamageableComponent : SharedDamageableComponent
    {
    }
}
