using Content.Shared.Implants;

namespace Content.Server.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        //TODO: Add events for doafter system
    }

    public void TryImplant()
    {
        //TODO: DoAfter logic will probably have to be on server
        //TODO: This is for implanting others, self should be instant inject
    }
}
