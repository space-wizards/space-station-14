// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Traps;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.DeadSpace.Traps;

public sealed class BearTrapVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprites = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BearTrapComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BearTrapComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnStartup(Entity<BearTrapComponent> ent, ref ComponentStartup args)
    {
        UpdateOpacity(ent);
    }

    private void OnAfterHandleState(Entity<BearTrapComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateOpacity(ent);
    }

    private void UpdateOpacity(Entity<BearTrapComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || MathHelper.CloseTo(sprite.Color.A, ent.Comp.Opacity))
            return;

        _sprites.SetColor((ent, sprite), sprite.Color.WithAlpha(ent.Comp.Opacity));
    }
}
