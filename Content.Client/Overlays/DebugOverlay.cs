using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public abstract class DebugOverlay<TPayload> : Overlay
    where TPayload : DebugOverlayPayload, new()
{
    public abstract void OnRecievedPayload(TPayload payload);
}
