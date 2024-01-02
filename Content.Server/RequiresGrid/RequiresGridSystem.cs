using Content.Server.Destructible;
using Robust.Server.GameObjects;

namespace Content.Server.RequiresGrid;

public sealed class RequiresGridSystem : EntitySystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RequiresGridComponent, EntParentChangedMessage>(OnEntParentChanged);
    }

    private void OnEntParentChanged(EntityUid owner, RequiresGridComponent component, EntParentChangedMessage args)
    {
        if (args.OldParent == null)
        {
            return;
        }

        var parent = _transformSystem.GetParent(owner);
        if (parent?.GridUid == null)
        {
            _destructible.DestroyEntity(owner);
        }
    }
}
