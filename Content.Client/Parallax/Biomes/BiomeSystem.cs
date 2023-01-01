using System.Diagnostics.CodeAnalysis;
using Content.Shared.Parallax.Biomes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Noise;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IClientTileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new BiomeOverlay(_tileDefManager, EntityManager, _mapManager, ProtoManager, _resource, this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        TileCache.Clear();
        _overlay.RemoveOverlay<BiomeOverlay>();
    }

    public bool TryGetDecal(
        Vector2 indices,
        FastNoise seed,
        float threshold,
        List<SpriteSpecifier> decals,
        [NotNullWhen(true)] out Texture? texture)
    {
        var value = (seed.GetCellular(indices.X, indices.Y) + 1f) / 2f;

        if (value <= threshold)
        {
            texture = null;
            return false;
        }

        var decal = Pick(decals, (seed.GetSimplex(indices.X, indices.Y) + 1f) / 2f);
        texture = _sprite.Frame0(decal);
        return true;
    }
}
