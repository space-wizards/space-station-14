using Content.Server.Salvage;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Clothing;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for checking if magnets-related xeno artifact node should be triggered.
/// Works with magboots and salvage magnet, salvage magnet triggers only upon pulsing on activation.
/// </summary>
public sealed class XATMagnetSystem : BaseQueryUpdateXATSystem<XATMagnetComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private HashSet<Entity<MagbootsComponent>> _magbootEntities = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SalvageMagnetActivatedEvent>(OnMagnetActivated);
    }

    /// <inheritdoc />
    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATMagnetComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        var coords = Transform(artifact.Owner).Coordinates;

        _magbootEntities.Clear();
        _lookup.GetEntitiesInRange(coords, node.Comp1.MagbootsRange, _magbootEntities);
        foreach (var ent in _magbootEntities)
        {
            if(!TryComp<ItemToggleComponent>(ent, out var itemToggle) || !itemToggle.Activated)
                continue;

            Trigger(artifact, node);
            break;
        }
    }

    private void OnMagnetActivated(ref SalvageMagnetActivatedEvent args)
    {
        var magnetCoordinates = Transform(args.Magnet).Coordinates;

        var query = EntityQueryEnumerator<XATMagnetComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            var artifact = _xenoArtifactQuery.Get(GetEntity(node.Attached.Value));

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoordinates = Transform(artifact).Coordinates;
            if (_transform.InRange(magnetCoordinates, artifactCoordinates, comp.MagnetRange))
                Trigger(artifact, (uid, comp, node));
        }
    }
}
