using JetBrains.Annotations;
using Content.Client.UserInterface.Controls;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechEquipmentRadialUIController : UIController
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SimpleRadialMenu? _menu;

    public void OpenRadialMenu(EntityUid mechEntity)
    {
        if (_menu != null)
            CloseMenu();

        if (!_entManager.TryGetComponent<MechComponent>(mechEntity, out var mechComp))
            return;

        _menu = new SimpleRadialMenu();
        var options = ConvertToButtons(mechComp);
        _menu.SetButtons(options);
        _menu.OpenCentered();
    }

    private List<RadialMenuOptionBase> ConvertToButtons(MechComponent mechComp)
    {
        var options = new List<RadialMenuOptionBase>
        {
            // Add "No Equipment" option
            new RadialMenuActionOption<string>(_ =>
                {
                    _entManager.RaisePredictiveEvent(new RequestMechEquipmentSelectEvent { Equipment = null });
                },
                "no_equipment")
            {
                ToolTip = Loc.GetString("mech-radial-no-equipment"),
                IconSpecifier = null
            }
        };

        // Add equipment options
        foreach (var equipment in mechComp.EquipmentContainer.ContainedEntities)
        {
            if (!_entManager.TryGetComponent<MetaDataComponent>(equipment, out var metaData))
                continue;

            var equipmentEntity = equipment;
            var tooltip = metaData.EntityName;
            SpriteSpecifier? sprite = null;

            if (metaData.EntityPrototype != null)
                sprite = new SpriteSpecifier.EntityPrototype(metaData.EntityPrototype.ID);

            options.Add(new RadialMenuActionOption<string>(_ =>
                {
                    _entManager.RaisePredictiveEvent(new RequestMechEquipmentSelectEvent
                        { Equipment = _entManager.GetNetEntity(equipmentEntity) });
                },
                metaData.EntityName)
            {
                ToolTip = tooltip,
                IconSpecifier = RadialMenuIconSpecifier.With(sprite)
            });
        }

        return options;
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Close();
        _menu = null;
    }
}
