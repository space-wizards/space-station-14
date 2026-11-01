using Content.Shared.GameTicking;
using Content.Shared.Mapping;
using Robust.Client.Graphics;

namespace Content.Client.Mapping;

public sealed partial class ClientRoofDebugOverlaySystem : RoofDebugOverlaySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    public readonly Dictionary<EntityUid, RoofDebugOverlayMessage> TileData = [];
    // }
    private RoofDebugOverlay? _overlay;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeNetworkEvent<RoofDebugOverlayMessage>(HandleRoofDebugOverlayMessage);
        SubscribeNetworkEvent<RoofDebugOverlayDisableMessage>(HandleRoofDebugOverlayDisableMessage);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);
    }
    private void OnGridRemoved(GridRemovalEvent ev)
    {
        if (TileData.ContainsKey(ev.EntityUid))
        {
            TileData.Remove(ev.EntityUid);
        }
    }
    private void HandleRoofDebugOverlayMessage(RoofDebugOverlayMessage message)
    {
        TileData[GetEntity(message.GridId)] = message;
        if (_overlay is not null)
            return;
        _overlay = new RoofDebugOverlay(this);
        _overlayManager.AddOverlay(_overlay);
    }
    private void HandleRoofDebugOverlayDisableMessage(RoofDebugOverlayDisableMessage ev)
    {
        TileData.Clear();
        RemoveOverlay();
    }
    public override void Shutdown()
    {
        base.Shutdown();
        RemoveOverlay();
    }
    public void Reset(RoundRestartCleanupEvent ev)
    {
        TileData.Clear();
    }
    public bool HasData(EntityUid gridId)
    {
        return TileData.ContainsKey(gridId);
    }
    private void RemoveOverlay()
    {
        if (_overlay is null)
            return;
        _overlayManager.RemoveOverlay(_overlay);
        _overlay = null;
    }
}
