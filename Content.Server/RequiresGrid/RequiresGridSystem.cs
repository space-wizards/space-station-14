using Content.Server.Destructible;

namespace Content.Server.RequiresGrid;

public sealed partial class RequiresGridSystem : EntitySystem
{
    [Dependency] private DestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RequiresGridComponent, EntParentChangedMessage>(OnEntParentChanged);
    }

    private void OnEntParentChanged(EntityUid owner, RequiresGridComponent component, EntParentChangedMessage args)
    {
        if (args.OldParent == null)
            return;

        if (args.Transform.GridUid != null)
            return;

        if (TerminatingOrDeleted(owner))
            return;

        _destructible.DestroyEntity(owner);
    }
}
