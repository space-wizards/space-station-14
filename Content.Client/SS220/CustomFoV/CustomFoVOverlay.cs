// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.SS220.CustomFoV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SS220.CustomFoV;

public sealed class CustomFoVOverlay : Overlay
{
    private readonly IEntityManager _entMan;
    private readonly IPrototypeManager _prototype;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly SpriteSpecifier _fovCorner;
    private readonly ShaderInstance _shader;

    internal CustomFoVOverlay(EntityManager entMan, IPrototypeManager prototype)
    {
        _entMan = entMan;
        _prototype = prototype;

        _fovCorner = new SpriteSpecifier.Texture(new("SS220/Misc/fov_corner.png"));
        _sprite = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _transform = _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _shader = _prototype.Index<ShaderPrototype>("unshaded").InstanceUnique();

        ZIndex = (int) Shared.DrawDepth.DrawDepth.WallFovOverlay;
    }

    private Dictionary<EntityUid, Dictionary<Vector2i, Entity<TransformComponent>>> _entMapDict = new();

    protected override void Draw(in OverlayDrawArgs args)
    {
        _entMapDict.Clear();

        if (args.Viewport.Eye is not { } eye || !eye.DrawFov)
            return;

        var eyeAngle = eye.Rotation;
        var texture = _sprite.Frame0(_fovCorner);
        var handle = args.WorldHandle;

        var customFovQuery = _entMan.AllEntityQueryEnumerator<FoVOverlayComponent>();
        var occluderQuery = _entMan.GetEntityQuery<OccluderComponent>();
        var xformQuery = _entMan.GetEntityQuery<TransformComponent>();

        while (customFovQuery.MoveNext(out var entity, out var comp))
        {
            if (!occluderQuery.TryGetComponent(entity, out var occluder) || !occluder.Enabled)
                continue;

            if (!xformQuery.TryGetComponent(entity, out var xform) || !xform.Anchored || !xform.GridUid.HasValue)
                continue;

            var gridComp = _entMan.GetComponent<MapGridComponent>(xform.GridUid.Value);
            var tile = gridComp.CoordinatesToTile(xform.Coordinates);

            // Create lookup maps for grids so neighbors can be found quickly
            if (!_entMapDict.TryGetValue(xform.GridUid.Value, out var gridDict))
            {
                gridDict = new();
                _entMapDict.Add(xform.GridUid.Value, gridDict);
            }

            if (gridDict.ContainsKey(tile))
                continue;

            var entityEntry = new Entity<TransformComponent>(entity, xform);
            gridDict.Add(tile, entityEntry);
        }

        handle.UseShader(_shader);

        void DrawFoVCorner(Vector2 worldPosition, Angle worldRot)
        {
            if (handle is null || texture is null)
                return;

            var rotatedMatrix = Matrix3.CreateTransform(worldPosition, worldRot);
            handle.SetTransform(rotatedMatrix);
            handle.DrawTexture(texture, new Vector2(-0.5f, -0.5f));
        }

        foreach (var (gridUid, objMap) in _entMapDict)
        {
            foreach (var (pos, entityEntry) in objMap)
            {
                var (worldPosition, _, worldMatrix) = _transform.GetWorldPositionRotationMatrix(entityEntry.Comp, xformQuery);
                var gridRot = _transform.GetWorldRotation(gridUid);

                Vector2i GetDirRelativeToEdge(Vector2i edge)
                {
                    var invGridTransform = Matrix3.CreateTransform(worldPosition, gridRot).Invert();
                    var relativePos = invGridTransform.Transform(eye!.Position.Position) + edge * 0.5f;
                    return new Vector2i(MathF.Sign(relativePos.X), MathF.Sign(relativePos.Y));
                }

                bool south_neighbour = objMap.ContainsKey(pos + Vector2i.Down);
                bool south_shadowed = GetDirRelativeToEdge(Vector2i.Up).Y > 0;
                bool south_obscured = south_shadowed || south_neighbour;

                bool north_neighbour = objMap.ContainsKey(pos + Vector2i.Up);
                bool north_shadowed = GetDirRelativeToEdge(Vector2i.Down).Y < 0;
                bool north_obscured = north_shadowed || north_neighbour;

                bool east_neighbour = objMap.ContainsKey(pos + Vector2i.Right);
                bool east_shadowed = GetDirRelativeToEdge(Vector2i.Left).X < 0;
                bool east_obscured = east_shadowed || east_neighbour;

                bool west_neighbour = objMap.ContainsKey(pos + Vector2i.Left);
                bool west_shadowed = GetDirRelativeToEdge(Vector2i.Right).X > 0;
                bool west_obscured = west_shadowed || west_neighbour;

                // SW corner
                if (south_obscured && west_obscured && !(!south_shadowed && !west_shadowed))
                {
                    DrawFoVCorner(worldPosition, gridRot + Angle.FromDegrees(0));
                }

                // SE corner
                if (south_obscured && east_obscured && !(!south_shadowed && !east_shadowed))
                {
                    DrawFoVCorner(worldPosition, gridRot + Angle.FromDegrees(90));
                }

                // NE corner
                if (north_obscured && east_obscured && !(!north_shadowed && !east_shadowed))
                {
                    DrawFoVCorner(worldPosition, gridRot + Angle.FromDegrees(180));
                }

                // NW corner
                if (north_obscured && west_obscured && !(!north_shadowed && !west_shadowed))
                {
                    DrawFoVCorner(worldPosition, gridRot + Angle.FromDegrees(270));
                }
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
        _entMapDict.Clear();
    }
}
