using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Guns
{
    public abstract class SharedGunSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;

        // TODO: Stuff. Play around and get prediction working first before doing SHEET.
    }
}
