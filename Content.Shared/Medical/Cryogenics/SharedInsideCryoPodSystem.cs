using Content.Shared.Standing;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem
{
    public virtual void InitializeInsideCryoPod()
    {
        SubscribeLocalEvent<InsideCryoPodComponent, DownAttemptEvent>(HandleDown);
    }

    // Must stand in the cryo pod
    private void HandleDown(EntityUid uid, InsideCryoPodComponent component, DownAttemptEvent args)
    {
        args.Cancel();
    }
}
