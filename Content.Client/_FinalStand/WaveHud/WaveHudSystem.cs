using Content.Shared._FinalStand.WaveHud;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;

namespace Content.Client._FinalStand.WaveHud;

public sealed class WaveHudSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private WaveHudOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<WaveCounterUpdateEvent>(OnWaveUpdate);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _overlay = null;
        }
    }

    private void OnWaveUpdate(WaveCounterUpdateEvent ev)
    {
        if (_overlay == null)
        {
            try
            {
                var textures = new Texture[10];
                for (var i = 0; i < 10; i++)
                    textures[i] = _resourceCache
                        .GetResource<TextureResource>(new ResPath($"/Textures/_FinalStand/WaveCounter/{i}.png"))
                        .Texture;

                _overlay = new WaveHudOverlay(textures);
                _overlayManager.AddOverlay(_overlay);
            }
            catch (Exception e)
            {
                Log.Error($"[WaveHud] Failed to load digit textures: {e.Message}");
                return;
            }
        }

        _overlay.CurrentWave = ev.Wave;
    }
}
