using Content.Shared.Nodes.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Nodes;

public sealed partial class NodeGraphSystem : SharedNodeGraphSystem
{
    [Dependency] private readonly IInputManager _inputMan = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IResourceCache _rscCache = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlayMan.AddOverlay(new NodeGraphOverlay(EntityManager, GameTiming, _inputMan, _uiMan, _xformSys, _rscCache));
    }

    public override void Shutdown()
    {
        _overlayMan.RemoveOverlay<NodeGraphOverlay>();

        base.Shutdown();
    }
}
