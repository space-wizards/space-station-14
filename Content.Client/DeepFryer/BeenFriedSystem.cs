using Content.Client.Interactable.Components;
using Content.Shared.DeepFryer;
using Content.Shared.Stealth.Components;
using Content.Shared.DeepFryer.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.DeepFryer;

public sealed class BeenFriedSystem : SharedBeenFriedSystem
{
    /*private static readonly ProtoId<ShaderPrototype> Shader = "Stealth";

    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index(Shader).InstanceUnique();

        SubscribeLocalEvent<BeenFriedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BeenFriedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BeenFriedComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    private void SetShader(EntityUid uid, bool enabled, BeenFriedComponent? component = null, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref component, ref sprite, false))
            return;

        _sprite.SetColor((uid, sprite), Color.White);
        sprite.PostShader = enabled ? _shader : null;
        sprite.GetScreenTexture = enabled;
        sprite.RaiseShaderEvent = enabled;
    }

    private void OnStartup(EntityUid uid, BeenFriedComponent component, ComponentStartup args)
    {
        SetShader(uid, true, component);
    }

    private void OnShutdown(EntityUid uid, BeenFriedComponent component, ComponentShutdown args)
    {
        if (!Terminating(uid))
            SetShader(uid, false, component);
    }

    private void OnShaderRender(EntityUid uid, BeenFriedComponent component, BeforePostShaderRenderEvent args)
    {
        // Distortion effect uses screen coordinates. If a player moves, the entities appear to move on screen. this
        // makes the distortion very noticeable.

        // So we need to use relative screen coordinates. The reference frame we use is the parent's position on screen.
        // this ensures that if the Stealth is not moving relative to the parent, its relative screen position remains
        // unchanged.
        var parent = Transform(uid).ParentUid;
        if (!parent.IsValid())
            return; // should never happen, but lets not kill the client.
        var parentXform = Transform(parent);
        var reference = args.Viewport.WorldToLocal(_transformSystem.GetWorldPosition(parentXform));
        reference.X = -reference.X;
        var visibility = 0.0f;

        // actual visual visibility effect is limited to +/- 1.
        visibility = Math.Clamp(visibility, -1f, 1f);

        _shader.SetParameter("reference", reference);
        _shader.SetParameter("visibility", visibility);

        visibility = MathF.Max(0, visibility);
        _sprite.SetColor((uid, args.Sprite), new Color(visibility, visibility, 1, 1));
    }*/
}
