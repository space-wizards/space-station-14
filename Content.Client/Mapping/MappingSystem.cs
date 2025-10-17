using Content.Client.Actions;
using Content.Shared.Actions;
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
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public static readonly EntProtoId SpawnAction = "BaseMappingSpawnAction";
    public static readonly EntProtoId EraserAction = "ActionMappingEraser";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FillActionSlotEvent>(OnFillActionSlot);
        SubscribeLocalEvent<StartPlacementActionEvent>(OnStartPlacementAction);
    }

    /// <summary>
    ///     This checks if the placement manager is currently active, and attempts to copy the placement information for
    ///     some entity or tile into an action. This is somewhat janky, but it seem to work well enough. Though I'd
    ///     prefer if it were to function more like DecalPlacementSystem.
    /// </summary>
    private void OnFillActionSlot(FillActionSlotEvent args)
    {
        if (!_placementMan.IsActive)
            return;

        if (args.Action != null)
            return;

        if (_placementMan.CurrentPermission is {} permission)
        {
            var ev = new StartPlacementActionEvent()
            {
                EntityType = permission.EntityType,
                PlacementOption = permission.PlacementOption,
            };

            var action = Spawn(SpawnAction);
            if (_placementMan.CurrentPermission.IsTile)
            {
                if (_tileMan[_placementMan.CurrentPermission.TileType] is not ContentTileDefinition tileDef)
                    return;

                if (!tileDef.MapAtmosphere && tileDef.Sprite is {} sprite)
                    _actions.SetIcon(action, new SpriteSpecifier.Texture(sprite));
                ev.TileId = tileDef.ID;
                _metaData.SetEntityName(action, Loc.GetString(tileDef.Name));
            }
            else if (permission.EntityType is {} id)
            {
                _actions.SetIcon(action, new SpriteSpecifier.EntityPrototype(id));
                _metaData.SetEntityName(action, id);
            }

            _actions.SetEvent(action, ev);
            args.Action = action;
        }
        else if (_placementMan.Eraser)
        {
            args.Action = Spawn(EraserAction);
        }
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
