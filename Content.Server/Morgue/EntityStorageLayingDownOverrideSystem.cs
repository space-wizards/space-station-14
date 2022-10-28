using Content.Server.Body.Systems;
using Content.Server.Morgue.Components;
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Standing;

namespace Content.Server.Morgue;

public sealed class EntityStorageLayingDownOverrideSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageLayingDownOverrideComponent, StorageBeforeCloseEvent>(OnBeforeClose);
    }

    private void OnBeforeClose(EntityUid uid, EntityStorageLayingDownOverrideComponent component,
        StorageBeforeCloseEvent args)
    {
        foreach (var ent in args.Contents)
            if (HasComp<BodyComponent>(ent) && !_standing.IsDown(ent))
                args.Contents.Remove(ent);
    }
}
