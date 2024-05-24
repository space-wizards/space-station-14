using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Movement.Systems;

public sealed class FloorOcclusionSystem : SharedFloorOcclusionSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FloorOcclusionComponent, ComponentStartup>(OnOcclusionStartup);
        SubscribeLocalEvent<FloorOcclusionComponent, AfterAutoHandleStateEvent>(OnOcclusionAuto);
    }

    private void OnOcclusionAuto(EntityUid uid, FloorOcclusionComponent component, ref AfterAutoHandleStateEvent args)
    {
        SetEnabled(uid, component, component.Enabled);
    }

    private void OnOcclusionStartup(EntityUid uid, FloorOcclusionComponent component, ComponentStartup args)
    {
        if (component.Enabled && TryComp<SpriteComponent>(uid, out var sprite))
            SetShader(sprite, true);
    }

    protected override void SetEnabled(EntityUid uid, FloorOcclusionComponent component, bool enabled)
    {
        if (component.Enabled == enabled)
            return;

        base.SetEnabled(uid, component, enabled);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        SetShader(sprite, enabled);
    }

    private void SetShader(SpriteComponent sprite, bool enabled)
    {
        var shader = _proto.Index<ShaderPrototype>("HorizontalCut").Instance();

        if (sprite.PostShader is not null && sprite.PostShader != shader)
            return;

        if (enabled)
        {
            sprite.PostShader = shader;
        }
        else
        {
            sprite.PostShader = null;
        }
    }
}
