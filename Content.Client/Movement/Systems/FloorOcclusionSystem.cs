using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Movement.Systems;

public sealed class FloorOcclusionSystem : SharedFloorOcclusionSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void SetEnabled(FloorOcclusionComponent component, bool enabled)
    {
        if (component.Enabled == enabled)
            return;

        base.SetEnabled(component, enabled);

        if (!TryComp<SpriteComponent>(component.Owner, out var sprite))
            return;

        if (enabled)
        {
            sprite.PostShader = _proto.Index<ShaderPrototype>("HorizontalCut").Instance();
        }
        else
        {
            sprite.PostShader = null;
        }
    }
}
