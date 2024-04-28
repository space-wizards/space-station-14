using Content.Client.Actions;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mapping;
using Content.Shared.Maps;
using Robust.Client.Placement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Mapping;

public sealed partial class MappingSystem : EntitySystem
{
    [Dependency] private readonly IPlacementManager _placementMan = default!;
    [Dependency] private readonly ITileDefinitionManager _tileMan = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    /// <summary>
    ///     The icon to use for space tiles.
    /// </summary>
    private readonly SpriteSpecifier _spaceIcon = new SpriteSpecifier.Texture(new ("Tiles/cropped_parallax.png"));

    /// <summary>
    ///     The icon to use for entity-eraser.
    /// </summary>
    private readonly SpriteSpecifier _deleteIcon = new SpriteSpecifier.Texture(new ("Interface/VerbIcons/delete.svg.192dpi.png"));

    public string DefaultMappingActions = "/mapping_actions.yml";

    [ValidatePrototypeId<EntityPrototype>]
    public string ActionStartPlacement = "ActionStartPlacement";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FillActionSlotEvent>(OnFillActionSlot);
        SubscribeLocalEvent<StartPlacementActionEvent>(OnStartPlacementAction);
    }

    public void LoadMappingActions()
    {
        _actions.LoadActionAssignments(DefaultMappingActions, false);
    }

    /// <summary>
    ///     This checks if the placement manager is currently active, and attempts to copy the placement information for
    ///     some entity or tile into an action. This is somewhat janky, but it seem to work well enough. Though I'd
    ///     prefer if it were to function more like DecalPlacementSystem.
    /// </summary>
    private void OnFillActionSlot(FillActionSlotEvent ev)
    {
        if (!_placementMan.IsActive)
            return;

        if (ev.Action != null)
            return;

        var actionEvent = new StartPlacementActionEvent();
        ITileDefinition? tileDef = null;

        if (_placementMan.CurrentPermission != null)
        {
            actionEvent.EntityType = _placementMan.CurrentPermission.EntityType;
            actionEvent.PlacementOption = _placementMan.CurrentPermission.PlacementOption;

            if (_placementMan.CurrentPermission.IsTile)
            {
                tileDef = _tileMan[_placementMan.CurrentPermission.TileType];
                actionEvent.TileId = tileDef.ID;
            }
        }
        else if (_placementMan.Eraser)
        {
            actionEvent.Eraser = true;
        }
        else
            return;

        string name;
        SpriteSpecifier icon;

        if (tileDef != null)
        {
            if (tileDef is not ContentTileDefinition contentTileDef)
                return;

            icon = contentTileDef.MapAtmosphere
                ? _spaceIcon
                : new SpriteSpecifier.Texture(contentTileDef.Sprite!.Value);

            name = Loc.GetString(tileDef.Name);
        }
        else if (actionEvent.Eraser)
        {
            icon = _deleteIcon;

            name = Loc.GetString("action-name-mapping-erase");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(actionEvent.EntityType))
                return;

            icon = new SpriteSpecifier.EntityPrototype(actionEvent.EntityType);

            name = actionEvent.EntityType;
        }

        var actionId = Spawn(ActionStartPlacement);
        var action = Comp<ActionComponent>(actionId);
        _actions.SetIcon((actionId, action), icon);
        _actions.SetEvent(actionId, actionEvent);
        _metaData.SetEntityName(actionId, name);

        ev.Action = actionId;
    }

    private void OnStartPlacementAction(StartPlacementActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _placementMan.BeginPlacing(new()
        {
            EntityType = args.EntityType,
            IsTile = args.TileId != null,
            TileType = args.TileId != null ? _tileMan[args.TileId].TileId : (ushort) 0,
            PlacementOption = args.PlacementOption,
        });

        if (_placementMan.Eraser != args.Eraser)
            _placementMan.ToggleEraser();
    }
}
