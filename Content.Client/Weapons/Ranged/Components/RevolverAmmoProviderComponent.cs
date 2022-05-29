using Content.Shared.Weapons.Ranged;

namespace Content.Client.Weapons.Ranged;

[RegisterComponent, ComponentReference(typeof(SharedRevolverAmmoProviderComponent))]
public sealed class RevolverAmmoProviderComponent : SharedRevolverAmmoProviderComponent
{
}
