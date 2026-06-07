using System.Numerics;
using System.Runtime.InteropServices;
using Content.Client.Graphics;
using Content.Client.Light.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Light.Components;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Light;

/// <summary>
/// Applies ambient-occlusion to the viewport.
/// </summary>
public sealed partial class AmbientOcclusionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilEqualDrawShader = "StencilEqualDraw";
    private const float BlurMultiplier = 7f;

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private readonly OverlayResourceCache<CachedResources> _resources = new ();
    private readonly OccluderSystem _occluders;
    private readonly GridStencilSystem _gridStencil;
    private readonly SharedTransformSystem _xformSystem;
    private readonly List<WorldTextureRect> _occluderQuads = new();

    private Color _color;

    public AmbientOcclusionOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 1;

        _occluders = _entManager.System<OccluderSystem>();
        _gridStencil = _entManager.System<GridStencilSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();

        _cfgManager.OnValueChanged(CCVars.AmbientOcclusionColor, OnColorChanged, true);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        /*
         * tl;dr
         * - we draw each occluder's bounds to an AO source texture.
         * - we blur this.
         * - We apply it to the viewport.
         *
         * We do this while ignoring lighting because it will wash out the actual effect.
         * In 3D ambient occlusion is more complicated due top having to calculate normals but in 2D
         * we don't have a concept of depth / corners necessarily.
         */

        var viewport = args.Viewport;
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var distance = _cfgManager.GetCVar(CCVars.AmbientOcclusionDistance);
        var resolutionScale = Math.Clamp(_cfgManager.GetCVar(CCVars.AmbientOcclusionResolutionScale), 0.1f, 1f);
        //var color = Color.Red;
        var target = viewport.RenderTarget;
        var aoSize = new Vector2i(
            Math.Max(1, (int) MathF.Ceiling(target.Size.X * resolutionScale)),
            Math.Max(1, (int) MathF.Ceiling(target.Size.Y * resolutionScale)));
        var lightScale = aoSize / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);
        var expandedBounds = worldBounds.Enlarged(GetBlurMargin(viewport, distance));
        var aoPadding = distance / EyeManager.PixelsPerMeter;

        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.AOTarget?.Texture.Size != aoSize)
        {
            res.AOTarget?.Dispose();
            res.AOTarget = _clyde.CreateRenderTarget(aoSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-target");
        }

        if (res.AOBlurBuffer?.Texture.Size != aoSize)
        {
            res.AOBlurBuffer?.Dispose();
            res.AOBlurBuffer = _clyde.CreateRenderTarget(aoSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-blur-target");
        }

        // Draw the texture data to the texture.
        args.WorldHandle.RenderInRenderTarget(res.AOTarget,
            () =>
            {
                worldHandle.UseShader(_proto.Index(UnshadedShader).Instance());
                var invMatrix = res.AOTarget.GetWorldToLocalMatrix(viewport.Eye!, scale);
                _occluderQuads.Clear();

                foreach (var entry in _occluders.QueryAabb(mapId, expandedBounds))
                {
                    DebugTools.Assert(entry.Component.Enabled);
                    var matrix = _xformSystem.GetWorldMatrix(entry.Transform);
                    var bounds = entry.Component.BoundingBox;
                    AddOccluderQuad(matrix, bounds.Enlarged(aoPadding));
                }

                worldHandle.SetTransform(invMatrix);
                worldHandle.DrawTextureRectsUnmodulated(Texture.White, CollectionsMarshal.AsSpan(_occluderQuads));
            }, Color.Transparent);

        _clyde.BlurRenderTarget(viewport, res.AOTarget, res.AOBlurBuffer, viewport.Eye!, BlurMultiplier);

        // Draw the stencil texture to depth buffer.
        var stencil = _gridStencil.GetNonSpaceStencil(args);
        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(stencil.Texture, worldBounds);

        // Draw the Blurred AO texture finally.
        var color = _entManager.TryGetComponent(args.MapUid, out MapAmbientColorComponent? mapAmbient)
            ? mapAmbient.Color
            : _color;

        worldHandle.UseShader(_proto.Index(StencilEqualDrawShader).Instance());
        worldHandle.DrawTextureRect(res.AOTarget!.Texture, worldBounds, color);

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);
    }

    private void OnColorChanged(string value)
    {
        _color = Color.FromHex(value);
    }

    private static float GetBlurMargin(IClydeViewport viewport, float distance)
    {
        if (viewport.Eye == null)
            return distance / EyeManager.PixelsPerMeter;

        var cameraSize = viewport.Eye.Zoom.Y * viewport.Size.Y * (1 / viewport.RenderScale.Y) / EyeManager.PixelsPerMeter;

        // Matches Clyde's BlurRenderTarget radius calculation closely enough to include off-screen AO contributors.
        return distance / EyeManager.PixelsPerMeter + BlurMultiplier / cameraSize;
    }

    private void AddOccluderQuad(in Matrix3x2 matrix, in Box2 bounds)
    {
        var origin = new Vector2(matrix.M31, matrix.M32);
        var rotation = new Angle(Math.Atan2(matrix.M12, matrix.M11));
        _occluderQuads.Add(new WorldTextureRect(new Box2Rotated(bounds.Translated(origin), rotation, origin)));
    }

    protected override void DisposeBehavior()
    {
        _cfgManager.UnsubValueChanged(CCVars.AmbientOcclusionColor, OnColorChanged);
        _resources.Dispose();

        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? AOTarget;
        public IRenderTexture? AOBlurBuffer;

        public void Dispose()
        {
            AOTarget?.Dispose();
            AOBlurBuffer?.Dispose();
        }
    }
}

