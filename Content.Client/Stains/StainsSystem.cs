using Content.Shared.Stains;
using Robust.Client.GameObjects;

namespace Content.Client.Stains;

public sealed class StainsSystem : SharedStainsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainableComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnAfterState(EntityUid uid, StainableComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }
        sprite.Color = component.StainColor;
    }
}
