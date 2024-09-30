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

        var protomanager = IoCManager.Resolve<IPrototypeManager>();
        var texture = _clyde.CreateLightRenderTarget(args.Viewport.LightRenderTarget.Size, name: "planet-lighting");
        var invMatrix = texture.GetWorldToLocalMatrix(args.Viewport.Eye, args.Viewport.RenderScale / 2f);
        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;

        /*
        args.WorldHandle.RenderInRenderTarget(texture,
            () =>
            {
                worldHandle.UseShader(protomanager.Index<ShaderPrototype>("StencilMask").Instance());
                worldHandle.SetTransform(invMatrix);
                worldHandle.DrawTextureRect(texture1.Texture, bounds, Color.Orange);
            }, null);
            */

        var entManager = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var viewport = args.Viewport;

        var lookup = entManager.System<EntityLookupSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();

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
                    worldHandle.UseShader(protomanager.Index<ShaderPrototype>("StencilMask").Instance());
                    worldHandle.SetTransform(matty);
                    SharedMapSystem.TilesEnumerator tileEnumerator;
                    {
                        worldHandle.UseShader(null);
                        tileEnumerator = mapSystem.GetTilesEnumerator(uid, grid, bounds);

                        while (tileEnumerator.MoveNext(out var tileRef))
                        {
                            if (tileRef.Tile.TypeId == 126)
                                continue;

                            var local = lookup.GetLocalBounds(tileRef, grid.TileSize);
                            worldHandle.DrawRect(local, Color.Blue);
                        }
                    }
                }, Color.Black);
        }

        // Copy texture to lighting buffer
        var lightTarget = args.Viewport.LightRenderTarget;
        var lightInvMatrix = lightTarget.GetWorldToLocalMatrix(args.Viewport.Eye, args.Viewport.RenderScale / 2f);

        args.WorldHandle.RenderInRenderTarget(args.Viewport.LightRenderTarget,
            () =>
            {
                worldHandle.SetTransform(lightInvMatrix);
                worldHandle.DrawTextureRect(texture.Texture, bounds);
            }, null);

        worldHandle.UseShader(null);
        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
