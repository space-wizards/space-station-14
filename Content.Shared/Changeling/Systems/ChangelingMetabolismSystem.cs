using Content.Shared.Changeling.Components;
using Content.Shared.Metabolism;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingMetabolizerSystem : EntitySystem
{
    [Dependency] private MetabolizerSystem _metabolizer = default!;

    [SubscribeLocalEvent]
    private void OnAfterTransform(Entity<AddMetabolismComponent> ent, ref AfterChangelingTransformEvent args)
    {
        if (ent.Comp.AddedMetabolizer == null)
            return;

        // Technically organs are the same after transforming, however at some point we will be cloning new ones.
        _metabolizer.AddMetabolizerToBody(ent, ent.Comp.AddedMetabolizer.Value);
    }
}
