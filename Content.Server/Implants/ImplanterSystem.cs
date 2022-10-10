using Content.Server.DoAfter;
using Content.Server.Explosion.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.MobState;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

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
