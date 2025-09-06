using Content.Client.Mech.Ui;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Systems;

public sealed class MechEquipmentRadialSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechComponent, MechOpenEquipmentRadialEvent>(OnOpenEquipmentRadial);
    }

    private void OnOpenEquipmentRadial(EntityUid uid, MechComponent component, MechOpenEquipmentRadialEvent args)
    {
        var controller = _uiManager.GetUIController<MechEquipmentRadialUIController>();
        controller.OpenRadialMenu(uid);
    }
}
