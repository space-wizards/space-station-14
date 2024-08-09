using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Shared.Sandboxing;

namespace Content.Client.Overlays;

public abstract class DebugOverlaySystem<TOverlay, TPayload> : SharedDebugOverlaySystem<TPayload>
    where TOverlay : DebugOverlay<TPayload>, new()
    where TPayload : DebugOverlayPayload, new()
{
    [Dependency] protected readonly IOverlayManager _overlayManager = default!;

    protected TOverlay? _currentOverlay = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TPayload>(HandleDebugOverlayPayload);
    }

    private void HandleDebugOverlayPayload(TPayload message)
    {
        if (message.OverlayEnabled)
        {
            if (_currentOverlay == null && !_overlayManager.HasOverlay<TOverlay>())
            {
                _currentOverlay = (TOverlay)_sandboxHelper.CreateInstance(typeof(TOverlay));
                _overlayManager.AddOverlay(_currentOverlay);
            }
            else if (_currentOverlay != null && !_overlayManager.HasOverlay<TOverlay>())
            {
                // is this case even possible? should I worry about it at all?
                throw new InvalidOperationException($"debug overlay `${nameof(TOverlay)}` exists but isn't added to the overlayManager?");
            }

            if (_currentOverlay != null && _overlayManager.HasOverlay<TOverlay>())
            {
                OnRecievedPayload(message);
                _currentOverlay.OnRecievedPayload(message);
            }
        }
        else
        {
            if (_currentOverlay == null && !_overlayManager.HasOverlay<TOverlay>())
            {
                throw new InvalidOperationException($"tried to remove debug overlay `${nameof(TOverlay)}` but an overlay of that type does not exist!");
            }
            else if (_currentOverlay != null && !_overlayManager.HasOverlay<TOverlay>())
            {
                // is this case even possible? should I worry about it at all?
                throw new InvalidOperationException($"debug overlay `${nameof(TOverlay)}` exists but isn't added to the overlayManager?");
            }
            else if (_currentOverlay != null && _overlayManager.HasOverlay<TOverlay>())
            {
                _overlayManager.RemoveOverlay<TOverlay>();
                _currentOverlay = null;
            }
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_currentOverlay != null && _overlayManager.HasOverlay<TOverlay>())
        {
            _overlayManager.RemoveOverlay<TOverlay>();
            _currentOverlay = null;
        }
    }

    /// <summary>
    /// The DebugOverlaySystem will be notified about receiving the payload first then the overlay will be notified
    /// </summary>
    /// <param name="message"></param>
    protected abstract void OnRecievedPayload(TPayload message);
}
