// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Traps;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Traps;

public sealed class BearTrapVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprites = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BearTrapComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var trap, out var sprite))
        {
            if (!MathHelper.CloseTo(sprite.Color.A, trap.Opacity))
                _sprites.SetColor((uid, sprite), sprite.Color.WithAlpha(trap.Opacity));
        }
    }
}
