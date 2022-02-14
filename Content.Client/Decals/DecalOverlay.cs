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

            Dictionary<string, SpriteSpecifier> cachedTextures = new();

            SpriteSpecifier GetSpriteSpecifier(string id)
            {
                if (cachedTextures.TryGetValue(id, out var spriteSpecifier))
                    return spriteSpecifier;

                spriteSpecifier = _prototypeManager.Index<DecalPrototype>(id).Sprite;
                cachedTextures.Add(id, spriteSpecifier);
                return spriteSpecifier;
            }

            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

            foreach (var (gridId, zIndexDictionary) in _decals.DecalRenderIndex)
            {
                var gridUid = _mapManager.GetGridEuid(gridId);
                var xform = xformQuery.GetComponent(gridUid);

                handle.SetTransform(_transform.GetWorldMatrix(xform));

                foreach (var (_, decals) in zIndexDictionary)
                {
                    foreach (var (_, decal) in decals)
                    {
                        var spriteSpecifier = GetSpriteSpecifier(decal.Id);

                        if (decal.Angle.Equals(Angle.Zero))
                            handle.DrawTexture(_sprites.Frame0(spriteSpecifier), decal.Coordinates, decal.Color);
                        else
                            handle.DrawTexture(_sprites.Frame0(spriteSpecifier), decal.Coordinates, decal.Angle, decal.Color);
                    }
                }
            }
        }
    }
}
