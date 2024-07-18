using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Shared.Foldable;

public sealed class DeployFoldableSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
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

        if (!TryComp(args.User, out HandsComponent? hands)
            || !_hands.TryDrop(args.User, args.Used, targetDropLocation: args.ClickLocation, handsComp: hands))
            return;

        if (!_foldable.TrySetFolded(ent, foldable, false))
        {
            _hands.TryPickup(args.User, args.Used, handsComp: hands);
            return;
        }

        args.Handled = true;
    }
}
