using System.Numerics;
using System.Runtime.InteropServices;
using Content.Client.Graphics;
using Content.Client.Light.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Light.Components;
using Robust.Shared.ComponentTrees;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
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

    private readonly List<Vector2> _aoVertices = new(4096);
    private readonly List<ushort> _aoIndices = new(6144);

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private readonly OverlayResourceCache<CachedResources> _resources = new ();
    private readonly OccluderSystem _occluders;
    private readonly GridStencilSystem _gridStencil;
    private readonly SharedTransformSystem _xformSystem;

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
         * - we draw each occluder's polygon to an AO source texture.
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
        var target = viewport.RenderTarget;
        var aoSize = new Vector2i(
            Math.Max(1, (int) MathF.Ceiling(target.Size.X * resolutionScale)),
            Math.Max(1, (int) MathF.Ceiling(target.Size.Y * resolutionScale)));
        var lightScale = aoSize / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);
        var expandedBounds = worldBounds.Enlarged(GetBlurMargin(viewport, distance));
        var polygonExpansion = distance / EyeManager.PixelsPerMeter;

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
                worldHandle.SetTransform(Matrix3x2.Identity);
                var worldToTargetMatrix = res.AOTarget.GetWorldToLocalMatrix(viewport.Eye!, scale);
                var state = new AmbientOcclusionQueryState(this, worldHandle, worldToTargetMatrix, polygonExpansion);

                _occluders.QueryAabb(ref state, static (ref AmbientOcclusionQueryState state, in ComponentTreeEntry<OccluderComponent> entry) =>
                {
                    state.Overlay.AppendAmbientOcclusionPolygon(entry, ref state);
                    return true;
                }, mapId, expandedBounds);

                FlushAmbientOcclusionPolygons(worldHandle);
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

    protected override void DisposeBehavior()
    {
        _cfgManager.UnsubValueChanged(CCVars.AmbientOcclusionColor, OnColorChanged);
        _resources.Dispose();

        base.DisposeBehavior();
    }

    private void AppendAmbientOcclusionPolygon(
        in ComponentTreeEntry<OccluderComponent> entry,
        ref AmbientOcclusionQueryState state)
    {
        DebugTools.Assert(entry.Component.Enabled);

        var localToTargetMatrix = GetLocalToTargetMatrix(entry, ref state);

        AppendAmbientOcclusionPolygon(
            state.WorldHandle,
            entry.Component.Polygon,
            localToTargetMatrix,
            state.Expansion);
    }

    private Matrix3x2 GetLocalToTargetMatrix(
        in ComponentTreeEntry<OccluderComponent> entry,
        ref AmbientOcclusionQueryState state)
    {
        // OccluderSystem's tree invariant is that occluders are parented directly to their map/grid tree.
        // In that case LocalMatrix is already local-to-tree, so avoid resolving a recursive world matrix per occluder.
        if (entry.Transform.ParentUid == entry.Component.TreeUid)
        {
            if (state.TreeUid != entry.Transform.ParentUid)
            {
                state.TreeUid = entry.Transform.ParentUid;
                state.TreeToTargetMatrix = Matrix3x2.Multiply(
                    _xformSystem.GetWorldMatrix(entry.Transform.ParentUid),
                    state.WorldToTargetMatrix);
            }

            return Matrix3x2.Multiply(entry.Transform.LocalMatrix, state.TreeToTargetMatrix);
        }

        return Matrix3x2.Multiply(_xformSystem.GetWorldMatrix(entry.Transform), state.WorldToTargetMatrix);
    }

    private void AppendAmbientOcclusionPolygon(
        DrawingHandleWorld worldHandle,
        ReadOnlySpan<Vector2> polygon,
        Matrix3x2 localToTargetMatrix,
        float expansion)
    {
        if (polygon.Length < 3)
            return;

        // Keep indices representable as ushort for DrawingHandleBase.DrawPrimitives().
        if (_aoVertices.Count + polygon.Length > ushort.MaxValue)
            FlushAmbientOcclusionPolygons(worldHandle);

        var indexBase = (ushort) _aoVertices.Count;
        var center = Vector2.Zero;

        for (var i = 0; i < polygon.Length; i++)
        {
            center += polygon[i];
        }

        center /= polygon.Length;

        for (var i = 0; i < polygon.Length; i++)
        {
            var vertex = polygon[i];

            if (expansion > 0f)
            {
                var offset = vertex - center;
                if (offset.LengthSquared() > 0f)
                    vertex += Vector2.Normalize(offset) * expansion;
            }

            _aoVertices.Add(Vector2.Transform(vertex, localToTargetMatrix));
        }

        for (var i = 1; i < polygon.Length - 1; i++)
        {
            _aoIndices.Add(indexBase);
            _aoIndices.Add((ushort) (indexBase + i));
            _aoIndices.Add((ushort) (indexBase + i + 1));
        }
    }

    private void FlushAmbientOcclusionPolygons(DrawingHandleWorld worldHandle)
    {
        if (_aoVertices.Count == 0)
            return;

        worldHandle.DrawPrimitives(
            DrawPrimitiveTopology.TriangleList,
            CollectionsMarshal.AsSpan(_aoIndices),
            CollectionsMarshal.AsSpan(_aoVertices),
            Color.White);

        _aoVertices.Clear();
        _aoIndices.Clear();
    }

    private struct AmbientOcclusionQueryState
    {
        public AmbientOcclusionOverlay Overlay;
        public DrawingHandleWorld WorldHandle;
        public Matrix3x2 WorldToTargetMatrix;
        public float Expansion;
        public EntityUid? TreeUid;
        public Matrix3x2 TreeToTargetMatrix;

        public AmbientOcclusionQueryState(
            AmbientOcclusionOverlay overlay,
            DrawingHandleWorld worldHandle,
            Matrix3x2 worldToTargetMatrix,
            float expansion)
        {
            Overlay = overlay;
            WorldHandle = worldHandle;
            WorldToTargetMatrix = worldToTargetMatrix;
            Expansion = expansion;
            TreeUid = null;
            TreeToTargetMatrix = Matrix3x2.Identity;
        }
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

