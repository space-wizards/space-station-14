using Content.Client.Interactable.Components;
using Content.Shared.Chameleon;
using Content.Shared.Chameleon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Chameleon;

public sealed class ChameleonSystem : SharedChameleonSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index<ShaderPrototype>("Chameleon").InstanceUnique();
        SubscribeLocalEvent<SharedChameleonComponent, ComponentInit>(OnAdd);
        SubscribeLocalEvent<SharedChameleonComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<SharedChameleonComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    private void OnAdd(EntityUid uid, SharedChameleonComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        sprite.PostShader = _shader;
        sprite.GetScreenTexture = true;
        sprite.RaiseShaderEvent = true;

        if (TryComp(uid, out InteractionOutlineComponent? outline))
        {
            RemComp(uid, outline);
            component.HadOutline = true;
        }
    }

    private void OnRemove(EntityUid uid, SharedChameleonComponent component, ComponentRemove args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        sprite.PostShader = null;
        sprite.GetScreenTexture = false;
        sprite.RaiseShaderEvent = false;

        if (component.HadOutline)
            AddComp<InteractionOutlineComponent>(uid);
    }

    private void OnShaderRender(EntityUid uid, SharedChameleonComponent component, BeforePostShaderRenderEvent args)
    {
        // Distortion effect uses screen coordinates. If a player moves, the entities appear to move on screen. this
        // makes the distortion very noticeable.

        // So we need to use relative screen coordinates. The reference frame we use is the parent's position on screen.
        // this ensures that if the chameleon is not moving relative to the parent, its relative screen position remains
        // unchanged.
        var parentXform = Transform(Transform(uid).ParentUid);
        var reference = args.Viewport.WorldToLocal(parentXform.WorldPosition);

        _shader.SetParameter("reference", reference);
        _shader.SetParameter("stealthLevel", component.StealthLevel);
        args.Sprite.Color = new Color(component.StealthLevel, component.StealthLevel, 1, 1);

        Dirty(component);
    }
}

