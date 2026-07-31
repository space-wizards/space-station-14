using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Metabolism;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingMetabolismSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private MetabolizerSystem _metabolizer = default!;

    [SubscribeLocalEvent]
    private void OnMetabolismInit(Entity<ChangelingMetabolismComponent> ent, ref MapInitEvent args)
    {
        AddMetabolizer(ent);
    }

    [SubscribeLocalEvent]
    private void OnAfterTransform(Entity<ChangelingMetabolismComponent> ent, ref AfterChangelingTransformEvent args)
    {
        // Technically organs are the same after transforming, however at some point we will be cloning new ones.
        AddMetabolizer(ent);
    }

    private void AddMetabolizer(Entity<ChangelingMetabolismComponent> ent)
    {
        var organs = _body.EnumerateOrgans<MetabolizerComponent>(ent.Owner);

        foreach (var organ in organs)
        {
            _metabolizer.TryAddMetabolizerType((organ.Owner, organ.Comp2), ent.Comp.AddedMetabolizer);
        }
    }
}
