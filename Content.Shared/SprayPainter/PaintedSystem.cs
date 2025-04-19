using Content.Shared.Examine;
using Content.Shared.SprayPainter.Components;
using Robust.Shared.Timing;

namespace Content.Shared.SprayPainter;

public sealed class PaintedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PaintedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<PaintedComponent> ent, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("spray-painter-on-examined-painted-message"));
    }

    public override void Update(float frameTime)
    {
        var paintedQuery = EntityQueryEnumerator<PaintedComponent>();
        while (paintedQuery.MoveNext(out var uid, out var component))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.NextUpdate >= component.RemoveTime)
            {
                _entityManager.RemoveComponent(uid, component);
                Dirty(uid, component);
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;
        }
    }
}
