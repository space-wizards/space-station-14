using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;

namespace Content.Server.UseWith;

public sealed class UseWithSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseWithComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, UseWithComponent component, InteractUsingEvent args)
    {
        if (component.UseWithWhitelist?.IsValid(args.Used) == false) return;
        
        for (var i = 0; i < component.SpawnCount; i++)
        {
            var result = Spawn(component.SpawnedPrototype, Transform(uid).Coordinates);
            result.RandomOffset(0.25f);
        }

        QueueDel(uid);
    }
}
