using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Decals.Overlays
{
    public sealed class DecalOverlay : Overlay
    {
        private readonly SpriteSystem _sprites;
        private readonly IEntityManager _entManager;
        private readonly IPrototypeManager _prototypeManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

        private readonly Dictionary<string, (Texture Texture, bool SnapCardinals)> _cachedTextures = new(64);

        public DecalOverlay(
            SpriteSystem sprites,
            IEntityManager entManager,
            IPrototypeManager prototypeManager)
        {
            _sprites = sprites;
            _entManager = entManager;
            _prototypeManager = prototypeManager;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            // Shouldn't need to clear cached textures unless the prototypes get reloaded.
            var handle = args.WorldHandle;
            var xformSystem = _entManager.System<TransformSystem>();
            var eyeAngle = args.Viewport.Eye?.Rotation ?? Angle.Zero;

            var gridQuery = _entManager.AllEntityQueryEnumerator<DecalGridComponent, TransformComponent>();

            while (gridQuery.MoveNext(out var decalGrid, out var xform))
            {
                if (xform.MapID != args.MapId)
                    continue;

                var zIndexDictionary = decalGrid.DecalRenderIndex;

                if (zIndexDictionary.Count == 0)
                    continue;

                var (_, worldRot, worldMatrix) = xformSystem.GetWorldPositionRotationMatrix(xform);

                handle.SetTransform(worldMatrix);

                foreach (var decals in zIndexDictionary.Values)
                {
                    foreach (var decal in decals.Values)
                    {
                        if (!_cachedTextures.TryGetValue(decal.Id, out var cache) && _prototypeManager.TryIndex<DecalPrototype>(decal.Id, out var decalProto))
                        {
                            cache = (_sprites.Frame0(decalProto.Sprite), decalProto.SnapCardinals);
                            _cachedTextures[decal.Id] = cache;
                        }

                        var cardinal = Angle.Zero;

                        if (cache.SnapCardinals)
                        {
                            var worldAngle = eyeAngle + worldRot;
                            cardinal = worldAngle.GetCardinalDir().ToAngle();
                        }

                        var angle = decal.Angle - cardinal;

                        if (angle.Equals(Angle.Zero))
                            handle.DrawTexture(cache.Texture, decal.Coordinates, decal.Color);
                        else
                            handle.DrawTexture(cache.Texture, decal.Coordinates, angle, decal.Color);
                    }
                }
            }

            handle.SetTransform(Matrix3.Identity);
        }
    }
}
