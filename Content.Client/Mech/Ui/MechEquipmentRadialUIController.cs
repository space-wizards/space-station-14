using Content.Client.UserInterface.Controls;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Client.GameObjects;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechEquipmentRadialUIController : UIController
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SimpleRadialMenu? _menu;
    private EntityUid? _currentMech;

    public MechEquipmentRadialUIController()
    {
        IoCManager.InjectDependencies(this);
    }

    public void OpenRadialMenu(EntityUid mechEntity)
    {
        if (_menu != null)
        {
            CloseMenu();
        }

        if (!_entManager.TryGetComponent<MechComponent>(mechEntity, out var mechComp))
            return;

        _currentMech = mechEntity;

        _menu = new SimpleRadialMenu();
        var options = ConvertToButtons(mechComp);
        _menu.SetButtons(options);
        _menu.OpenCentered();
    }

    private IEnumerable<RadialMenuOption> ConvertToButtons(MechComponent mechComp)
    {
        var options = new List<RadialMenuOption>();

        // Add "No Equipment" option
        options.Add(new RadialMenuActionOption<string>(data =>
        {
            _entManager.RaisePredictiveEvent(new RequestMechEquipmentSelectEvent { Equipment = null });
        }, "no_equipment")
        {
            ToolTip = Loc.GetString("mech-radial-no-equipment"),
            Sprite = null
        });

        // Add equipment options
        foreach (var equipment in mechComp.EquipmentContainer.ContainedEntities)
        {
            if (!_entManager.TryGetComponent<MetaDataComponent>(equipment, out var metaData))
                continue;

            var equipmentEntity = equipment;

            string tooltip = metaData.EntityName;
            SpriteSpecifier? sprite = null;

            // Prefer tool quality icon if present
            if (_entManager.TryGetComponent<ToolComponent>(equipment, out var toolComp))
            {
                foreach (var quality in toolComp.Qualities)
                {
                    if (_prototypeManager.TryIndex(quality, out ToolQualityPrototype? qualityProto))
                    {
                        tooltip = qualityProto.Name;
                        if (qualityProto.Icon != null && qualityProto.Icon != SpriteSpecifier.Invalid)
                        {
                            sprite = qualityProto.Icon;
                        }
                    }
                    break;
                }
            }

            // Fallback to equipment prototype sprite icon
            if (sprite == null && metaData.EntityPrototype != null)
            {
                sprite = new SpriteSpecifier.EntityPrototype(metaData.EntityPrototype.ID);
            }

            options.Add(new RadialMenuActionOption<string>(data =>
            {
                _entManager.RaisePredictiveEvent(new RequestMechEquipmentSelectEvent { Equipment = _entManager.GetNetEntity(equipmentEntity) });
            }, metaData.EntityName)
            {
                ToolTip = tooltip,
                Sprite = sprite
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
        _currentMech = null;
    }
}
