using System.Collections.Generic;
using Content.Shared.Decals;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Maths;

namespace Content.Client.Decals
{
    public class DecalOverlay : Overlay
    {
        private readonly DecalSystem _system;
        private readonly IMapManager _mapManager;
        private readonly IPrototypeManager _prototypeManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

        public DecalOverlay(DecalSystem system, IMapManager mapManager, IPrototypeManager prototypeManager)
        {
            _system = system;
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

            foreach (var (gridId, zIndexDictionary) in _system.DecalRenderIndex)
            {
                var grid = _mapManager.GetGrid(gridId);
                handle.SetTransform(grid.WorldMatrix);
                foreach (var (_, decals) in zIndexDictionary)
                {
                    foreach (var (_, decal) in decals)
                    {
                        var spriteSpecifier = GetSpriteSpecifier(decal.Id);
                        handle.DrawTexture(spriteSpecifier.Frame0(), decal.Coordinates, decal.Angle, decal.Color);
                    }
                }
            }
        }
    }
}
