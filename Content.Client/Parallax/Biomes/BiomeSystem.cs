using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new BiomeOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<BiomeOverlay>();
    }

    // For now we'll just use 

    public Tile GetTile(Vector2i indices, BiomePrototype prototype, int seed)
    {
        return Tile.Empty;
    }

    public Texture GetTileTexture(Vector2i indices, BiomePrototype prototype, int seed)
    {

    }
}
