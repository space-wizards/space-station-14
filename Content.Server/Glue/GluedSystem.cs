using Content.Shared.Interaction.Components;
using Robust.Shared.Timing;
using Content.Shared.Glue;
using Content.Shared.Hands;

namespace Content.Server.Glue;

public sealed class GluedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GluedComponent, ComponentInit>(OnGluedInit);
        SubscribeLocalEvent<GluedComponent, GotEquippedHandEvent>(OnHandPickUp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GluedComponent, UnremoveableComponent>();
        while (query.MoveNext(out var uid, out var glue, out _))
        {
            if (_timing.CurTime < glue.Until)
                continue;

            _metaData.SetEntityName(uid, glue.BeforeGluedEntityName);
            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);
        }
    }

    private void OnGluedInit(EntityUid uid, GluedComponent component, ComponentInit args)
    {
        var meta = MetaData(uid);
        var name = meta.EntityName;
        component.BeforeGluedEntityName = meta.EntityName;
        _metaData.SetEntityName(uid, Loc.GetString("glued-name-prefix", ("target", name)));
    }

    private void OnHandPickUp(EntityUid uid, GluedComponent component, GotEquippedHandEvent args)
    {
        EnsureComp<UnremoveableComponent>(uid);
        component.Until = _timing.CurTime + component.Duration;
    }
}
