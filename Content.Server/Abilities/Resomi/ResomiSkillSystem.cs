using Content.Server.Actions;
using Content.Shared.Abilities.Resomi;
using Content.Shared.Throwing;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;

namespace Content.Server.Abilities.Resomi;

public sealed class ResomiSkillSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiSkillComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ResomiSkillComponent, ResomiJumpActionEvent>(OnResomiJump);
    }
    
    private void OnStartup(EntityUid uid, ResomiSkillComponent component, ComponentStartup args) => _action.AddAction(uid, component.ActionJumpId);
    
    private void OnResomiJump(EntityUid uid, ResomiSkillComponent component, ResomiJumpActionEvent args)
    {
        //idea taked from VigersRay
        
        if (args.Handled)
            return;
        
        var userTransform = Transform(uid);
        var userPosition = _transform.GetMapCoordinates(userTransform);
        
        if (!_mapMan.TryFindGridAt(userPosition, out _, out var grid))
            return;
        
        if (!_mapSystem.TryGetTileRef(uid, grid, userTransform.Coordinates, out var tileRef))
            return;
        
        if (tileRef.Tile.IsEmpty || tileRef.IsSpace() || tileRef.Tile.GetContentTileDefinition().ID == "Lattice")
            return;

        args.Handled = true;
        
        var targetPlace = args.Target.ToMap(EntityManager, _transform);
        
        var targetDirection = targetPlace.Position - userTransform.MapPosition.Position;

        if (targetDirection.Length() > component.MaxThrow)
            targetDirection = targetDirection.Normalized() * component.MaxThrow;

        _throwing.TryThrow(uid, targetDirection, 7F, uid, 10F);
    }
}
