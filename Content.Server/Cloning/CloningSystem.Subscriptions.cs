using Content.Server.Zombies;
using Content.Shared.Cloning.Events;
using Content.Shared.Zombies;

namespace Content.Server.Cloning;

/// <summary>
/// The part of item cloning responsible for copying over important components.
/// </summary>
/// <remarks>
/// This is separate from the CloningSystem to place cloning logic closer together.
/// To exclude or add any specific copy code, place this in cloning context.
/// </remarks>
public sealed partial class CloningSystem
{
    [Dependency] private ZombieSystem _zombie = default!;

    [SubscribeLocalEvent]
    private void OnZombieCloned(Entity<ZombieComponent> ent, ref ClonedEvent args)
    {
        // Return the original's appearance to how it was before being zombified.
        _zombie.UnZombify(ent, args.CloneUid, ent.Comp);
    }
}
