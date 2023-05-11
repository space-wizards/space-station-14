using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Interaction.Components;
using Content.Shared.Actions;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Medical;

namespace Content.Server.Glue;

public sealed class GluedSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GluedComponent, ComponentInit>(OnGlued);
    }

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

            glue.Enabled = true;
            glue.Glued = false;
            glue.GlueBroken = false;
            MetaData(uid).EntityName = glue.BeforeGluedEntityName;
            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);
        }
    }

    private void OnGlued(EntityUid uid, GluedComponent component, ComponentInit args)
    {
        var meta = MetaData(uid);
        var name = meta.EntityName;
        component.BeforeGluedEntityName = meta.EntityName;
        _audio.PlayPvs(component.Squeeze, uid);
        meta.EntityName = Loc.GetString("glued-name-prefix", ("target", name));
        component.Enabled = false;
        component.GlueBroken = true;
        component.GlueTime = _timing.CurTime + component.GlueCooldown;
    }
}
