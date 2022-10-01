using Content.Client.Chameleon.Components;
using Content.Client.Interactable.Components;
using Content.Shared.Chameleon;
using Content.Shared.Chameleon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client.Chameleon;

public sealed class ChameleonSystem : SharedChameleonSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        _shader = _protoMan.Index<ShaderPrototype>("Chameleon").InstanceUnique();
        SubscribeLocalEvent<ChameleonComponent, ComponentInit>(OnAdd);
        SubscribeLocalEvent<ChameleonComponent, ComponentRemove>(OnRemove);
        SubscribeNetworkEvent<ChameleonUpdateEvent>(OnChameleonUpdate);
        SubscribeLocalEvent<ChameleonComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    private void OnChameleonUpdate(ChameleonUpdateEvent ev)
    {
        if (TryComp<ChameleonComponent>(ev.Owner, out var chameleon))
        {
            chameleon.HadOutline = ev.HadOutline;
            chameleon.Speed = ev.Speed;
        }
    }

    private void OnAdd(EntityUid uid, ChameleonComponent component, ComponentInit args)
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
        Dirty(component);
    }

    private void OnRemove(EntityUid uid, ChameleonComponent component, ComponentRemove args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        sprite.PostShader = null;
        sprite.GetScreenTexture = false;
        sprite.RaiseShaderEvent = false;

        if (component.HadOutline)
            AddComp<InteractionOutlineComponent>(uid);
    }

    private void OnShaderRender(EntityUid uid, ChameleonComponent component, BeforePostShaderRenderEvent args)
    {
        // Distortion effect uses screen coordinates. If a player moves, the entities appear to move on screen. this
        // makes the distortion very noticeable.

        // So we need to use relative screen coordinates. The reference frame we use is the parent's position on screen.
        // this ensures that if the chameleon is not moving relative to the parent, its relative screen position remains
        // unchanged.
        var parentXform = Transform(Transform(uid).ParentUid);
        var reference = args.Viewport.WorldToLocal(parentXform.WorldPosition);

        _shader.SetParameter("reference", reference);
        _shader.SetParameter("speed", component.Speed);
        args.Sprite.Color = new Color(component.Speed, component.Speed, 1, 1);
        Dirty(component);
    }
}

