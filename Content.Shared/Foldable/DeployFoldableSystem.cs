using Content.Shared.Buckle;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Shared.Foldable;

public sealed class DeployFoldableSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly FoldableSystem _foldable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeployFoldableComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<DeployFoldableComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!TryComp<FoldableComponent>(ent, out var foldable))
            return;

        if (!_hands.TryDrop(args.User, args.Used))
            return;

        _transform.SetCoordinates(args.Used, args.ClickLocation);

        if (!_foldable.TrySetFolded(ent, foldable, false))
            return;

        args.Handled = true;
    }
}
