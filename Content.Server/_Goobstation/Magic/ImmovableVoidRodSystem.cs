using Content.Server.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.Maps;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Map.Components;

namespace Content.Server.Magic;

public sealed partial class ImmovableVoidRodSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IEntityManager _ent = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // we are deliberately including paused entities. rod hungers for all
        foreach (var (rod, trans) in EntityManager.EntityQuery<ImmovableVoidRodComponent, TransformComponent>(true))
        {
            rod.Accumulator += frameTime;

            if (rod.Accumulator > rod.Lifetime.TotalSeconds)
            {
                QueueDel(rod.Owner);
                return;
            }

            if (!_ent.TryGetComponent<MapGridComponent>(trans.GridUid, out var grid))
                continue;



            var tileref = grid.GetTileRef(trans.Coordinates);
            var tile = _prot.Index<ContentTileDefinition>("FloorAstroSnow");
            _tile.ReplaceTile(tileref, tile);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImmovableVoidRodComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<ImmovableVoidRodComponent> ent, ref StartCollideEvent args)
    {
        if ((TryComp<HereticComponent>(args.OtherEntity, out var th) && th.CurrentPath == "Void")
        || HasComp<GhoulComponent>(args.OtherEntity))
            return;

        _stun.TryParalyze(args.OtherEntity, TimeSpan.FromSeconds(2.5f), false);

        TryComp<TagComponent>(args.OtherEntity, out var tag);
        var tags = tag?.Tags ?? new();

        if (tags.Contains("Wall") && Prototype(args.OtherEntity) != null && Prototype(args.OtherEntity)!.ID != "WallSnowCobblebrick")
        {
            Spawn("WallSnowCobblebrick", Transform(args.OtherEntity).Coordinates);
            QueueDel(args.OtherEntity);
        }
    }
}
