using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Metabolism;
using Robust.Shared.Containers;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingMetabolismSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MetabolizerSystem _metabolizer = default!;

    [SubscribeLocalEvent]
    private void OnBiodegradeAction(Entity<ChangelingMetabolismComponent> ent, ref MapInitEvent args)
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
        if (!_container.TryGetContainer(ent, BodyComponent.ContainerID, out var container))
        {
            Log.Error($"Entity {ToPrettyString(ent)} with a {nameof(ChangelingMetabolismComponent)} is missing a container ({BodyComponent.ContainerID}).");
            return;
        }

        foreach (var organ in container.ContainedEntities)
        {
            _metabolizer.TryAddMetabolizerType(organ, ent.Comp.AddedMetabolizer);
        }
    }
}
