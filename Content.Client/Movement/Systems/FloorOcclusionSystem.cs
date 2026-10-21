using Content.Client.Graphics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Movement.Systems;

public sealed partial class FloorOcclusionSystem : SharedFloorOcclusionSystem
{
    private static readonly ProtoId<ShaderPrototype> HorizontalCut = "HorizontalCut";

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloorOcclusionComponent, ComponentStartup>(OnOcclusionStartup);
        SubscribeLocalEvent<FloorOcclusionComponent, ComponentShutdown>(OnOcclusionShutdown);
        SubscribeLocalEvent<FloorOcclusionComponent, AfterAutoHandleStateEvent>(OnOcclusionAuto);
    }

    private void OnOcclusionAuto(Entity<FloorOcclusionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        SetShader(ent.Owner, ent.Comp.Enabled);
    }

    private void OnOcclusionStartup(Entity<FloorOcclusionComponent> ent, ref ComponentStartup args)
    {
        SetShader(ent.Owner, ent.Comp.Enabled);
    }

    private void OnOcclusionShutdown(Entity<FloorOcclusionComponent> ent, ref ComponentShutdown args)
    {
        SetShader(ent.Owner, false);
    }

    protected override void SetEnabled(Entity<FloorOcclusionComponent> entity)
    {
        SetShader(entity.Owner, entity.Comp.Enabled);
    }

    private void SetShader(Entity<SpriteComponent?> sprite, bool enabled)
    {
        if (!_spriteQuery.Resolve(sprite.Owner, ref sprite.Comp, false))
            return;

        var shader = ProtoMan.Index(HorizontalCut).Instance();

        if (enabled)
        {
            _sprite.SetPostShader(sprite, new SpriteComponent.PostShaderArgs(ContentPostShaderIds.FloorOcclusion, shader)
            {
                Before = ContentPostShaderIds.BeforeOutlines,
            });
        }
        else
        {
            _sprite.RemovePostShader(sprite, ContentPostShaderIds.FloorOcclusion);
        }
    }
}
