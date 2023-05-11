using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Interaction.Components;
using Content.Shared.Actions;
using Content.Shared.Glue;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Content.Server.Chemistry.EntitySystems;

namespace Content.Server.Glue;

public sealed class GluedSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GluedComponent, ComponentStartup>(OnGlued);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GluedComponent>();
        while (query.MoveNext(out var uid, out var glued))

            foreach (var glue in EntityQuery<GluedComponent>())
            {
                if (glue.Glued)
                    continue;

                if (_timing.CurTime < glue.GlueCooldown)
                    continue;

                glue.Glued = true;

                RemoveGlue(uid, glued);
            }
    }

    private void OnGlued(EntityUid uid, GluedComponent component, ComponentStartup args)
    {
        var meta = MetaData(uid);
        var name = meta.EntityName;
        component.BeforeGluedEntityName = meta.EntityName;
        _audio.PlayPvs(component.Squeeze, uid);
        meta.EntityName = Loc.GetString("glued-name-prefix", ("target", name));
        component.Glued = false;
    }

    private void RemoveGlue(EntityUid uid, GluedComponent component)
    {
        if (component.Glued == true)
        {
            MetaData(uid).EntityName = component.BeforeGluedEntityName;
            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);
            component.GlueCooldown = TimeSpan.FromSeconds(30);
        }
    }
}
