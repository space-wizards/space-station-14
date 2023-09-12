using Content.Client.Nodes.Components;
using Content.Client.Nodes.Overlays;
using Content.Shared.Nodes;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Nodes.EntitySystems;

public sealed partial class NodeGraphSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IInputManager _inputMan = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IResourceCache _rscCache = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GraphNodeComponent, ComponentHandleState>(HandleComponentState);
        SubscribeLocalEvent<NodeGraphComponent, ComponentHandleState>(HandleComponentState);

        SubscribeNetworkEvent<EnableNodeVisMsg>(OnEnableNodeVisMsg);
    }

    public override void Shutdown()
    {
        if (_visEnabled)
            _overlayMan.RemoveOverlay<DebugNodeVisualsOverlay>();
    }
}
