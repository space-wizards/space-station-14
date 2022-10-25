using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.SimpleMob;

namespace Content.Server.SimpleMob;

public sealed class SimpleMobSystem : SharedSimpleMobSystem
{
    [Dependency] private readonly EntityStorageSystem _storage = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SimpleMobComponent, InteractNoHandEvent>(OnSimpleMobInteract);
    }

    private void OnSimpleMobInteract(EntityUid uid, SimpleMobComponent component, InteractNoHandEvent args)
    {
        //free the mice
        if (TryComp<EntityStorageComponent>(args.Target, out var storage) || storage != null && !storage.Open && storage.Contents.Contains(args.User))
            _storage.OpenStorage(args.Target);
    }
}
