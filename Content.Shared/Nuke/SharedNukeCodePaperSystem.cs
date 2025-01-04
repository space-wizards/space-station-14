using Content.Shared.Pinpointer;

namespace Content.Shared.Nuke;

public abstract class SharedNukeCodePaperSystem : EntitySystem
{
    [Dependency] private readonly SharedPinpointerSystem _pinpointerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeCodePaperComponent, GotPinpointerScannedEvent>(GotPinpointerScanned);
    }

    ///<summary>
    ///     Stores the linked nuke on the paper
    /// </summary>
    public void LinkNuke(EntityUid uid, EntityUid nukeUid, NukeCodePaperComponent nukeCodePaper)
    {
        nukeCodePaper.Nuke = nukeUid;
        Dirty(uid, nukeCodePaper);
    }

    /// <summary>
    ///     Sets the target of the pinpointer to the nuke that belongs to the code on the paper.
    /// </summary>
    private void GotPinpointerScanned(EntityUid uid, NukeCodePaperComponent component, GotPinpointerScannedEvent args)
    {
        if (component.Nuke == null)
            return;

        _pinpointerSystem.SetTarget(args.Pinpointer, component.Nuke, args.Component, args.User, true);
        _pinpointerSystem.StoreTarget(component.Nuke, args.Pinpointer, args.Component, args.User);
        args.Handled = true;
    }
}
