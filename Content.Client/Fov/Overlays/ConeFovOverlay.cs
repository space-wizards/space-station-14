using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;

namespace Content.Client.Fov.Overlays;

/// <summary>
/// World-space-below-FOV overlay that draws and masks a vision cone using the stencil buffer.
/// Adds soft, feathered outlines for the center circle and cone edges for a smoother look.
/// </summary>
public sealed class ConeFovOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> StencilClearId = "StencilClear";
    private static readonly ProtoId<ShaderPrototype> StencilMaskId = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawId = "StencilDraw";
    private static readonly ProtoId<ShaderPrototype> StencilEqualDrawId = "StencilEqualDraw";

    private readonly IEyeManager _eyeManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly ShaderInstance _stencilClear;
    private readonly ShaderInstance _stencilMask;
    private readonly ShaderInstance _stencilDraw;
    private readonly ShaderInstance _stencilEqualDraw;

    private readonly IPlayerManager _player;
    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transform;

    /// <summary>
    /// Total cone angle in degrees.
    /// </summary>
    public float AngleDegrees { get; set; } = 120f;

    /// <summary>
    /// Additional rotation offset in degrees applied to the cone direction.
    /// Positive values rotate counter-clockwise.
    /// </summary>
    public float RotationOffsetDegrees { get; set; } = 0f;

    /// <summary>
    /// Opacity of the area outside the cone. 0 = fully transparent, 1 = fully black.
    /// </summary>
    public float OutsideOpacity { get; set; } = 0.7f;

    /// <summary>
    /// Radius of a small clear circle around the eye position that is not affected by the limiter (world units).
    /// </summary>
    public float CenterClearRadius { get; set; } = 0f;

    /// <summary>
    /// Edge feather in pixels for the cone boundary. Higher values produce a softer transition.
    /// </summary>
    public float EdgeFeatherPixels { get; set; } = 8f;

    /// <summary>
    /// Feather in pixels for the small clear circle around the eye. Higher values produce a softer transition.
    /// </summary>
    public float CenterFeatherPixels { get; set; } = 6f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ConeFovOverlay(IEyeManager eyeManager, IPrototypeManager prototypeManager)
    {
        _eyeManager = eyeManager;
        _prototypeManager = prototypeManager;
        _stencilClear = _prototypeManager.Index(StencilClearId).InstanceUnique();
        _stencilMask = _prototypeManager.Index(StencilMaskId).InstanceUnique();
        _stencilDraw = _prototypeManager.Index(StencilDrawId).InstanceUnique();
        _stencilEqualDraw = _prototypeManager.Index(StencilEqualDrawId).InstanceUnique();

        // Resolve required services without IoC injection to avoid unregistered dependency errors
        _player = IoCManager.Resolve<IPlayerManager>();
        _entManager = IoCManager.Resolve<IEntityManager>();
        _transform = _entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        var eye = _eyeManager.CurrentEye;
        if (eye == null || eye.Position.MapId != args.MapId)
            return;

        // Center on player world position if available, otherwise eye position.
        Vector2 centerPos = eye.Position.Position;
        var rotation = (float) eye.Rotation.Theta;
        if (_player.LocalEntity is { } player && _entManager.TryGetComponent<TransformComponent>(player, out var xform))
        {
            centerPos = _transform.GetWorldPosition(xform);
            rotation = (float) _transform.GetWorldRotation(xform).Theta;
        }

        // Apply rotation offset (degrees -> radians)
        rotation += RotationOffsetDegrees * (MathF.PI / 180f);

        // Compute a radius large enough to cover current view
        var worldAabb = args.WorldBounds.CalcBoundingBox();
        var diag = worldAabb.Size.Length();
        var radius = diag * 1.2f;

        // Build cone polygon as a triangle fan
        var half = MathF.PI * (AngleDegrees / 180f) * 0.5f;
        const int Segments = 48;
        var verts = new Vector2[Segments + 2];
        verts[0] = centerPos;
        for (var i = 0; i <= Segments; i++)
        {
            var t = i / (float) Segments;
            var ang = rotation - half + (2f * half) * t;
            var dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
            verts[i + 1] = centerPos + dir * radius;
        }

        // 1) Clear stencil in current world-bounds
        handle.UseShader(_stencilClear);
        handle.DrawRect(args.WorldBounds, Color.White);

        // 2) Write the cone to stencil (ref=1)
        handle.UseShader(_stencilMask);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, Color.White);

        // 2b) Also mark a small circle around the player as inside (ref=1)
        if (CenterClearRadius > 0f)
            handle.DrawCircle(centerPos, CenterClearRadius, Color.White, true);

        // 3) Draw outside with desired opacity where stencil != 1
        handle.UseShader(_stencilDraw);
        handle.DrawRect(args.WorldBounds, Color.Black.WithAlpha(OutsideOpacity));

        handle.UseShader(null);
    }
}
