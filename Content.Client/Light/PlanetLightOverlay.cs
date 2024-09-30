using System.Numerics;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

public sealed class PlanetLightOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public PlanetLightOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var viewport = args.Viewport;
        var eye = args.Viewport.Eye;
        var entManager = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var protomanager = IoCManager.Resolve<IPrototypeManager>();
        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;

        var lookup = entManager.System<EntityLookupSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();

        var fovTexture = _clyde.CreateRenderTarget(viewport.RenderTarget.Size,
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "planet-fov");
        var texture = _clyde.CreateLightRenderTarget(viewport.LightRenderTarget.Size, name: "planet-lighting");

        args.WorldHandle.RenderInRenderTarget(fovTexture,
            () =>
            {
                var invMatrix = fovTexture.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);
                worldHandle.SetTransform(invMatrix);
                _clyde.ApplyFovToBuffer(viewport, fovTexture, eye, Color.Black);
            }, Color.White);

        args.WorldHandle.RenderInRenderTarget(texture,
            () =>
            {
                var shader = protomanager.Index<ShaderPrototype>("StencilMask").Instance();
                worldHandle.UseShader(shader);
                var invMatrix = texture.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);
                worldHandle.SetTransform(invMatrix);
                worldHandle.DrawTextureRect(fovTexture.Texture, bounds);
            }, null);

        var invMatrix = texture.GetWorldToLocalMatrix(args.Viewport.Eye, args.Viewport.RenderScale / 2f);

        var query = entManager.AllEntityQueryEnumerator<PlanetLightComponent, MapGridComponent, TransformComponent>();
        // TODO: Render to a separate texture, blur, then apply to the main texture.

        while (query.MoveNext(out var uid, out var comp, out var grid, out var xform))
        {
            if (args.MapId != xform.MapID)
                continue;

            var gridMatrix = xformSystem.GetWorldMatrix(uid);

            var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

            args.WorldHandle.RenderInRenderTarget(texture,
                () =>
                {
                    worldHandle.UseShader(protomanager.Index<ShaderPrototype>("StencilDraw").Instance());
                    worldHandle.SetTransform(matty);
                    SharedMapSystem.TilesEnumerator tileEnumerator;
                    {
                        tileEnumerator = mapSystem.GetTilesEnumerator(uid, grid, bounds);

                        while (tileEnumerator.MoveNext(out var tileRef))
                        {
                            if (tileRef.Tile.TypeId == 126)
                                continue;

                            var local = lookup.GetLocalBounds(tileRef, grid.TileSize);
                            worldHandle.DrawRect(local, Color.Blue);
                        }
                    }
                }, null);
        }

        // Copy texture to lighting buffer
        worldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                var lightInvMatrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(eye, viewport.RenderScale / 2f);
                worldHandle.SetTransform(lightInvMatrix);
                worldHandle.DrawTextureRect(texture.Texture, bounds);
            }, null);

        worldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
