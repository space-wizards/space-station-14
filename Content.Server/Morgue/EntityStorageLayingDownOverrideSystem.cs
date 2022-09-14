using Content.Server.Morgue.Components;
using Content.Shared.Standing;
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;

namespace Content.Server.Morgue;

public sealed class EntityStorageLayingDownOverrideSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageLayingDownOverrideComponent, StorageBeforeCloseEvent>(OnBeforeClose);
    }

    private void OnBeforeClose(EntityUid uid, EntityStorageLayingDownOverrideComponent component, StorageBeforeCloseEvent args)
    {
        foreach (var ent in args.Contents)
            if (HasComp<SharedBodyComponent>(ent) && !_standing.IsDown(ent))
                args.Contents.Remove(ent);
    }
}
