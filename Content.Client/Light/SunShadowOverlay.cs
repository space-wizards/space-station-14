using System.Numerics;
using Content.Client.Light.Components;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Light;

public sealed class SunShadowOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _xformSys;

    private readonly HashSet<Entity<SunShadowCastComponent>> _shadows = new();

    private IRenderTexture? _target;

    public SunShadowOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xformSys = _entManager.System<SharedTransformSystem>();
        _lookup = _entManager.System<EntityLookupSystem>();
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entManager.TryGetComponent(args.MapUid, out SunShadowComponent? sun))
            return;

        var direction = sun.Direction;
        var alpha = Math.Clamp(sun.Alpha, 0f, 1f);

        // Nowhere to cast to so ignore it.
        if (direction.Equals(Vector2.Zero) || alpha == 0f)
            return;

        var length = direction.Length();
        var worldHandle = args.WorldHandle;
        var viewport = args.Viewport;
        var eye = viewport.Eye;
        var mapId = args.MapId;

        if (eye == null)
            return;

        // TODO: Fix jittering (imprecision due to matrix maths?)
        // Likely need to get all loca lcoords and shit.
        // Also looks like they stretch on right side of the screen quite badly, check PR.

        // Feature todo: dynamic shadows for mobs and trees. Also ideally remove the fake tree shadows.


        var worldBounds = args.WorldBounds;
        var expandedBounds = worldBounds.Enlarged(length + 0.1f);
        _shadows.Clear();

        if (_target?.Size != viewport.LightRenderTarget.Size)
        {
            _target = _clyde
                .CreateRenderTarget(viewport.LightRenderTarget.Size,
                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "sun-shadow-target");
        }

        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);

        // Draw shadow polys to stencil
        args.WorldHandle.RenderInRenderTarget(_target,
            () =>
            {
                var invMatrix =
                    _target.GetWorldToLocalMatrix(eye, scale);
                var indices = new Vector2[PhysicsConstants.MaxPolygonVertices];

                // Go through shadows in range.

                // For each one we:
                // - Get the original vertices.
                // - Extrapolate these along the sun direction.
                // - Combine the above into 1 single polygon to draw.

                // Note that this is range-limited for accuracy; if you set it too high it will clip through walls or other undesirable entities.
                // This is probably not noticeable most of the time but if you want something "accurate" you'll want to code a solution.
                // Ideally the CPU would have its own shadow-map copy that we could just ray-cast each vert into though
                // You might need to batch verts or the likes as this could get expensive.
                _lookup.GetEntitiesIntersecting(mapId, expandedBounds, _shadows);

                foreach (var ent in _shadows)
                {
                    var xform = _entManager.GetComponent<TransformComponent>(ent.Owner);
                    var worldMatrix = _xformSys.GetWorldMatrix(xform);
                    var renderMatrix = Matrix3x2.Multiply(worldMatrix, invMatrix);

                    indices[0] = new Vector2(-0.5f, -0.5f);
                    indices[1] = new Vector2(0.5f, -0.5f);
                    indices[2] = new Vector2(0.5f, 0.5f);
                    indices[3] = new Vector2(-0.5f, 0.5f);

                    indices[4] = indices[0] + direction;
                    indices[5] = indices[1] + direction;
                    indices[6] = indices[2] + direction;
                    indices[7] = indices[3] + direction;

                    var hull = PhysicsHull.ComputeHull(indices, 8);
                    worldHandle.SetTransform(renderMatrix);

                    worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, hull.Points[..hull.Count], Color.White);
                }
            }, Color.Transparent);

        // Draw stencil (see roofoverlay).
        args.WorldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                var invMatrix =
                    viewport.LightRenderTarget.GetWorldToLocalMatrix(eye, scale);
                worldHandle.SetTransform(invMatrix);

                var maskShader = _protoManager.Index<ShaderPrototype>("Mix").Instance();
                worldHandle.UseShader(maskShader);

                worldHandle.DrawTextureRect(_target.Texture, worldBounds, Color.Black.WithAlpha(alpha));
            }, null);
    }
}
