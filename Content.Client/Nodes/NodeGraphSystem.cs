using Content.Shared.Nodes.EntitySystems;
using Robust.Client.Graphics;

namespace Content.Client.Nodes;

public sealed partial class NodeGraphSystem : SharedNodeGraphSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlayMan.AddOverlay(new NodeGraphOverlay(EntityManager, _xformSys));
    }

    public override void Shutdown()
    {
        _overlayMan.RemoveOverlay<NodeGraphOverlay>();

        base.Shutdown();
    }
}
