using Content.Shared.Weapons.Ranged;

namespace Content.Server.Weapon.Ranged;

[RegisterComponent, ComponentReference(typeof(SharedRevolverAmmoProviderComponent))]
public sealed class RevolverAmmoProviderComponent : SharedRevolverAmmoProviderComponent
{

}
