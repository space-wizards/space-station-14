using System.Numerics;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class SpawnArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public const string NodeDataSpawnAmount = "nodeDataSpawnAmount";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, SpawnArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (!_artifact.TryGetNodeData(uid, NodeDataSpawnAmount, out int amount))
            amount = 0;

        if (amount >= component.MaxSpawns)
            return;

        if (component.Spawns is not {} spawns)
            return;

        var artifactCord = _transform.GetMapCoordinates(uid);
        foreach (var spawn in EntitySpawnCollection.GetSpawns(spawns, _random))
        {
            var dx = _random.NextFloat(-component.Range, component.Range);
            var dy = _random.NextFloat(-component.Range, component.Range);
            var spawnCord = artifactCord.Offset(new Vector2(dx, dy));
            var ent = Spawn(spawn, spawnCord);
            _transform.AttachToGridOrMap(ent);

            //#IMP random chance to make ghost role
            if (_random.NextFloat() < component.GhostRoleProb && !HasComp<GhostRoleComponent>(ent))
            {
                if (!TryComp<MetaDataComponent>(ent, out var meta))
                    continue;

                // Markers should not be ghost roles
                if (meta.EntityPrototype is {} proto && proto.Parents is {} parents && parents.Contains("MarkerBase"))
                    continue;

                var grComp = EnsureComp<GhostRoleComponent>(ent);
                grComp.RoleName = meta.EntityName;
                grComp.RoleDescription = meta.EntityDescription;
                EnsureComp<GhostTakeoverAvailableComponent>(ent);
            }
        }
        _artifact.SetNodeData(uid, NodeDataSpawnAmount, amount + 1);
    }
}
