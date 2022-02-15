using Content.Client.Weapons.Ranged.Barrels.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Barrels.EntitySystems;

public sealed class ClientBatteryBarrelSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClientBatteryBarrelComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, ClientBatteryBarrelComponent component, ref AppearanceChangeEvent args)
    {
        component.ItemStatus?.Update(args.Component);
    }
}
