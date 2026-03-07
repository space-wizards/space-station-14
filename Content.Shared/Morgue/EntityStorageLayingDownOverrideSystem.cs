using Content.Shared.Morgue.Components;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;

namespace Content.Shared.Morgue;

public sealed class EntityStorageLayingDownOverrideSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageLayingDownOverrideComponent, StorageBeforeCloseEvent>(OnBeforeClose);
    }

    private void OnBeforeClose(EntityUid uid, EntityStorageLayingDownOverrideComponent component, ref StorageBeforeCloseEvent args)
    {
        foreach (var ent in args.Contents)
        {
            // Explicitly check for standing state component, as entities without it will return false for IsDown()
            // which prevents inserting any kind of non-mobs into this container (which is unintended)
            if (TryComp<StandingStateComponent>(ent, out var standingState) && !_standing.IsDown((ent, standingState)))
                args.Contents.Remove(ent);
        }
    }
}
