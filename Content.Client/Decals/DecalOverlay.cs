using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Decals
{
    public sealed class DecalOverlay : Overlay
    {
        private readonly DecalSystem _decals;
        private readonly SharedTransformSystem _transform;
        private readonly SpriteSystem _sprites;
        private readonly IEntityManager _entManager;
        private readonly IMapManager _mapManager;
        private readonly IPrototypeManager _prototypeManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

        public DecalOverlay(
            DecalSystem decals,
            SharedTransformSystem transforms,
            SpriteSystem sprites,
            IEntityManager entManager,
            IMapManager mapManager,
            IPrototypeManager prototypeManager)
        {
            _decals = decals;
            _transform = transforms;
            _sprites = sprites;
            _entManager = entManager;
            _mapManager = mapManager;
            _prototypeManager = prototypeManager;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.WorldHandle;

            Dictionary<string, Texture> cachedTextures = new();

            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

            foreach (var (gridId, zIndexDictionary) in _decals.DecalRenderIndex)
            {
                var gridUid = _mapManager.GetGridEuid(gridId);
                var xform = xformQuery.GetComponent(gridUid);

                handle.SetTransform(_transform.GetWorldMatrix(xform, xformQuery));

                foreach (var (_, decals) in zIndexDictionary)
                {
                    foreach (var (_, decal) in decals)
                    {
                        if (!cachedTextures.TryGetValue(decal.Id, out var texture))
                        {
                            var sprite = _prototypeManager.Index<DecalPrototype>(decal.Id).Sprite;
                            texture = _sprites.Frame0(sprite);
                            cachedTextures[decal.Id] = texture;
                        }

                        if (decal.Angle.Equals(Angle.Zero))
                            handle.DrawTexture(texture, decal.Coordinates, decal.Color);
                        else
                            handle.DrawTexture(texture, decal.Coordinates, decal.Angle, decal.Color);
                    }
                }
            }
        }
    }
}
