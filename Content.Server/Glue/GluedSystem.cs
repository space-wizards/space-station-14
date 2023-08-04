using Content.Shared.Interaction.Components;
using Robust.Shared.Timing;
using Content.Shared.Interaction;
using Content.Shared.Glue;
using Content.Shared.Hands.Components;

namespace Content.Server.Glue;

public sealed class GluedSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GluedComponent, ComponentInit>(OnGluedInit);
        SubscribeLocalEvent<GluedComponent, InteractHandEvent>(OnPickUp);
    }

    // Timing to remove glued and unremoveable.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);


        var query = EntityQueryEnumerator<GluedComponent>();
        while (query.MoveNext(out var uid, out var glue))
        {
            if (!glue.GlueBroken || glue.Glued)
                continue;

            if (_timing.CurTime < glue.GlueTime)
                continue;

            glue.Glued = false;
            glue.GlueBroken = false;
            MetaData(uid).EntityName = glue.BeforeGluedEntityName;
            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);
        }
    }

    //Adds the prefix on init.
    private void OnGluedInit(EntityUid uid, GluedComponent component, ComponentInit args)
    {
        var meta = MetaData(uid);
        var name = meta.EntityName;
        component.BeforeGluedEntityName = meta.EntityName;
        meta.EntityName = Loc.GetString("glued-name-prefix", ("target", name));
    }

    // Timers start only when the glued item is picked up.
    private void OnPickUp(EntityUid uid, GluedComponent component, InteractHandEvent args)
    {
        var userHands = Comp<HandsComponent>(args.User);
        if (userHands.ActiveHandEntity == uid)
        {
            component.GlueBroken = true;
            component.GlueTime = _timing.CurTime + component.GlueCooldown;
        }
    }
}
