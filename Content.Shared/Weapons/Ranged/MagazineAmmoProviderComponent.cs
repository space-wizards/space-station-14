using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent]
public sealed class MagazineAmmoProviderComponent : AmmoProviderComponent
{
    [ViewVariables, DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    public ContainerSlot Magazine = default!;
}
