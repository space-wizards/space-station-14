using Content.Server.Salvage;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Clothing;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

public sealed class XATMagnetSystem : BaseQueryUpdateXATSystem<XATMagnetComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SalvageMagnetActivatedEvent>(OnMagnetActivated);
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

    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATMagnetComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        var query = EntityQueryEnumerator<MagbootsComponent, ItemToggleComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var itemToggle, out var xform))
        {
            if (!itemToggle.Activated)
                continue;

            if (!_transform.InRange(xform.Coordinates, Transform(artifact).Coordinates, node.Comp1.MagbootsRange))
                continue;

            Trigger(artifact, node);
            break;
        }
    }
}
