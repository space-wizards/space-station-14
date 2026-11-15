using System.Numerics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

public sealed partial class InitialBodySystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private OrganRelationSystem _organRelation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InitialBodyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<InitialBodyComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ContainerManagerComponent>(ent, out var containerComp))
            return;

        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        if (!_container.TryGetContainer(ent, BodyComponent.ContainerID, out var container, containerComp))
        {
            Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(InitialBodyComponent)} is missing a container ({BodyComponent.ContainerID}).");
            return;
        }

        var xform = Transform(ent);
        var coords = new EntityCoordinates(ent, Vector2.Zero);
        var spawned = new Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid>();

        foreach (var (part, proto) in ent.Comp.Organs)
        {
            // TODO: When e#6192 is merged replace this all with TrySpawnInContainer...
            var spawn = Spawn(proto, coords);

            if (!_container.Insert(spawn, container, containerXform: xform))
            {
                Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(InitialBodyComponent)} failed to insert an entity: {ToPrettyString(spawn)}.\n");
                Del(spawn);
                continue;
            }

            spawned[part] = spawn;
        }

        if (ent.Comp.Relationships is null)
            return;

        foreach (var (partId, parentUid) in spawned)
        {
            if (!ent.Comp.Relationships.TryGetValue(partId, out var children))
                continue;

            foreach (var childId in children)
            {
                if (!spawned.TryGetValue(childId, out var childUid))
                    continue;

                _organRelation.Relate(parentUid, childUid);
            }
        }
    }
}
