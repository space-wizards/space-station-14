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
/// World-space-below-FOV overlay that draws a fully black mask outside a vision cone.
/// Implemented using stencil shaders to avoid custom GLSL. Outside is 100% black by default.
/// </summary>
public sealed class ConeFovOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> StencilClearId = "StencilClear";
    private static readonly ProtoId<ShaderPrototype> StencilMaskId = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawId = "StencilDraw";

    private readonly IEyeManager _eyeManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly ShaderInstance _stencilClear;
    private readonly ShaderInstance _stencilMask;
    private readonly ShaderInstance _stencilDraw;

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
    public float CenterClearRadius { get; set; } = 0.75f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ConeFovOverlay(IEyeManager eyeManager, IPrototypeManager prototypeManager)
    {
        _eyeManager = eyeManager;
        _prototypeManager = prototypeManager;
        _stencilClear = _prototypeManager.Index(StencilClearId).InstanceUnique();
        _stencilMask = _prototypeManager.Index(StencilMaskId).InstanceUnique();
        _stencilDraw = _prototypeManager.Index(StencilDrawId).InstanceUnique();

        // Resolve required services without IoC injection to avoid unregistered dependency errors
        _player = IoCManager.Resolve<IPlayerManager>();
        _entManager = IoCManager.Resolve<IEntityManager>();
        _transform = _entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        // Validate eye
        var eye = _eyeManager.CurrentEye;
        if (eye == null || eye.Position.MapId != args.MapId)
            return;

        // Eye position and rotation
        var eyePos = eye.Position.Position;
        var rotation = (float) eye.Rotation.Theta;
        // Prefer controlled entity world rotation so the cone matches the player's facing.
        if (_player.LocalEntity is { } player && _entManager.TryGetComponent<TransformComponent>(player, out var xform))
        {
            rotation = (float) _transform.GetWorldRotation(xform).Theta;
        }

        // Apply user-configurable rotation offset (degrees -> radians)
        rotation += RotationOffsetDegrees * (MathF.PI / 180f);

        // Compute cone polygon as triangle fan
        var worldAabb = args.WorldBounds.CalcBoundingBox();
        var diag = worldAabb.Size.Length();
        var radius = diag * 1.2f;

        var half = MathF.PI * (AngleDegrees / 180f) * 0.5f;
        const int Segments = 48;
        var verts = new Vector2[Segments + 2];
        verts[0] = eyePos;
        for (var i = 0; i <= Segments; i++)
        {
            var t = i / (float) Segments;
            var ang = rotation - half + (2f * half) * t;
            var dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
            verts[i + 1] = eyePos + dir * radius;
        }

        // 1) Clear stencil in current world-bounds
        handle.UseShader(_stencilClear);
        handle.DrawRect(args.WorldBounds, Color.White);

        // 2) Write the cone to stencil (ref=1) using a triangle fan
        handle.UseShader(_stencilMask);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, Color.White);

        // 2b) Also mark a small circle around the eye as inside (ref=1)
        if (CenterClearRadius > 0f)
        {
            handle.DrawCircle(eyePos, CenterClearRadius, Color.White, true);
        }

        // 3) Draw transparent black where stencil != 1 (outside the cone)
        handle.UseShader(_stencilDraw);
        handle.DrawRect(args.WorldBounds, Color.Black.WithAlpha(OutsideOpacity));

        handle.UseShader(null);
    }
}
