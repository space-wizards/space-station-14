using Content.Client.Eui;
using Robust.Client.Graphics;

namespace Content.Client.NPC;

public sealed class NPCSystem : EntitySystem
{
    /*
     * Mainly handles clientside debugging
     */
    public bool EnableOverlay
    {
        get => _enableOverlay;
        set
        {
            var overlayManager = IoCManager.Resolve<IOverlayManager>();
            _enableOverlay = value;

            if (_enableOverlay)
            {
                overlayManager.AddOverlay(new HTNOverlay());
            }
            else
            {
                overlayManager.RemoveOverlay<HTNOverlay>();
            }
        }
    }

    private bool _enableOverlay;
}
