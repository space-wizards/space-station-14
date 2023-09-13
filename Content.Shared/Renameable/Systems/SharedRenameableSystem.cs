using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Renameable;
using Content.Shared.Renameable.Components;
using Content.Shared.Tools;
using Content.Shared.Wires;

namespace Content.Shared.Renameable.Systems;

/// <summary>
/// Handles opening rename dialog when a tool is used, with the maints panel closed.
/// </summary>
public abstract class SharedRenameableSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedWiresSystem _wires = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenameableComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, RenameableComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_tool.HasQuality(args.Used, comp.Quality))
            return;

        if (_wires.IsClosed(uid))
            return;

        if (!_blocker.CanInteract(args.User, uid))
            return;

        args.Handled = TryOpen(uid, args.User);
    }

    /// <summary>
    /// Opens the ui on the server.
    /// Client predicts this as always opening.
    /// </summary>
    protected abstract bool TryOpen(EntityUid uid, EntityUid user);
}
