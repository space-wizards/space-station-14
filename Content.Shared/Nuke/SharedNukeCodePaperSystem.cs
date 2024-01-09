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

    /// <summary>
    ///     Sets the target of the pinpointer to the nuke that belongs to the code on the paper.
    /// </summary>
    private void GotPinpointerScanned(EntityUid uid, NukeCodePaperComponent component, GotPinpointerScannedEvent args)
    {
        if (component.Nuke != null)
        {
            _pinpointerSystem.SetTarget(args.Pinpointer, component.Nuke);
        }
    }
}
