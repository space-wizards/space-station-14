using System.Collections;
using Content.Shared.Fluids;
using Robust.Client.Graphics;

namespace Content.Client.Fluids;

public sealed class PuddleDebugOverlaySystem : SharedPuddleDebugOverlaySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public readonly Dictionary<EntityUid, PuddleOverlayDebugMessage> TileData = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PuddleOverlayDisableMessage>(DisableOverlay);
        SubscribeNetworkEvent<PuddleOverlayDebugMessage>(RenderDebugData);

        if (!_overlayManager.HasOverlay<PuddleOverlay>())
        {
            _overlayManager.AddOverlay(new PuddleOverlay());
        }
    }

    private void RenderDebugData(PuddleOverlayDebugMessage message)
    {
        TileData[message.GridUid] = message;
    }

    private void DisableOverlay(PuddleOverlayDisableMessage message)
    {
        TileData.Clear();
    }

    public bool HasData(EntityUid gridId)
    {
        return TileData.ContainsKey(gridId);
    }

    public PuddleDebugOverlayData[] GetData(EntityUid mapGridGridEntityId)
    {
        return TileData[mapGridGridEntityId].OverlayData;
    }
}
