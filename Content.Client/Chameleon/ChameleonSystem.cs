using Content.Client.Chameleon.Components;
using Content.Client.Interactable.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Chameleon;

public sealed class ChameleonSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        _shader = _protoMan.Index<ShaderPrototype>("Chameleon").InstanceUnique();
        SubscribeLocalEvent<ChameleonComponent, ComponentInit>(OnAdd);
        SubscribeLocalEvent<ChameleonComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<ChameleonComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ChameleonComponent, BeforePostShaderRenderEvent>(OnShaderRender);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var chameleon in EntityQuery<ChameleonComponent>())
        {
            chameleon.Speed = Math.Clamp(chameleon.Speed - frameTime * 0.15f, -1f, 1f);
        }
    }

    private void OnRemove(EntityUid uid, ChameleonComponent component, ComponentRemove args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        sprite.PostShader = null;
        sprite.LayerSetShader(0, "unshaded");
        sprite.GetScreenTexture = false;
        sprite.RaiseShaderEvent = false;

        if (component.HadOutline)
            AddComp<InteractionOutlineComponent>(uid);
    }

    private void OnAdd(EntityUid uid, ChameleonComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        sprite.PostShader = _shader;
        sprite.LayerSetShader(0, "chameleon");
        sprite.GetScreenTexture = true;
        sprite.RaiseShaderEvent = true;

        if (TryComp(uid, out InteractionOutlineComponent? outline))
        {
            RemComp(uid, outline);
            component.HadOutline = true;
        }
    }

    private void OnMove(EntityUid uid, ChameleonComponent component, ref MoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.NewPosition.EntityId != args.OldPosition.EntityId)
            return;

        component.Speed += 0.2f*(args.NewPosition.Position - args.OldPosition.Position).Length;
        component.Speed = Math.Clamp(component.Speed, -1f, 1f);
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
    }
}

