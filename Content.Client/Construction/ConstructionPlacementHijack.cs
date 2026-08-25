using System.Linq;
using Content.Shared.Construction.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction;

public sealed partial class ConstructionPlacementHijack : PlacementHijack
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;

    private EntityQuery<ConstructionGhostComponent> _constructionGhostQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    private readonly ConstructionSystem _constructionSystem;

    private readonly ConstructionPrototype? _prototype;

    public ConstructionPrototype? CurrentPrototype => _prototype;

    public override bool CanRotate { get; }

    public ConstructionPlacementHijack(ConstructionPrototype? prototype)
    {
        IoCManager.InjectDependencies(this);

        _constructionSystem = _entMan.System<ConstructionSystem>();
        _constructionGhostQuery = _entMan.GetEntityQuery<ConstructionGhostComponent>();
        _spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();
        _prototype = prototype;
        CanRotate = prototype?.CanRotate ?? true;
    }

    /// <inheritdoc />
    public override bool HijackPlacementRequest(EntityCoordinates coordinates)
    {
        if (_prototype != null)
        {
            var dir = Manager.Direction;
            _constructionSystem.SpawnGhost(_prototype, coordinates, dir);
        }
        return true;
    }

    /// <inheritdoc />
    public override bool HijackDeletion(EntityUid entity)
    {
        if (_constructionGhostQuery.HasComp(entity))
            _constructionSystem.ClearGhost(entity.GetHashCode());
        return true;
    }

    /// <inheritdoc />
    public override void StartHijack(PlacementManager manager)
    {
        base.StartHijack(manager);

        if (_prototype is null || !_constructionSystem.TryGetRecipePrototype(_prototype.ID, out var targetProtoId))
            return;

        if (!_protoMan.HasIndex(targetProtoId))
            return;

        // Spawn our entity, get its SpriteComponent
        var targetUid = _entMan.Spawn(targetProtoId);
        try
        {
            var sprite = _spriteQuery.Comp(targetUid);

            manager.PreparePlacementSprite((targetUid, sprite));
        }
        finally
        {
            _entMan.DeleteEntity(targetUid);
        }
    }
}
