using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged;

[RegisterComponent]
public sealed class ChamberMagazineAmmoProviderComponent : AmmoProviderComponent
{
    [ViewVariables, DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    public ContainerSlot Chamber = default!;

    public ContainerSlot Magazine = default!;
}
