using Content.Server.Botany.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;

namespace Content.Server.Botany.Systems;

public sealed class LogSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LogComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, LogComponent component, InteractUsingEvent args)
    {
        if (_tags.HasTag(args.Used, "BotanySharp"))
        {
            for (var i = 0; i < component.SpawnCount; i++)
            {
                var plank = Spawn(component.SpawnedPrototype, Transform(uid).Coordinates);
                plank.RandomOffset(0.25f);
            }

            QueueDel(uid);
        }
    }
}
