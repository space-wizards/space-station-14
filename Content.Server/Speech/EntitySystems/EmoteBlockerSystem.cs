using Content.Server.Speech.Components;
using Content.Shared.Emoting;
using Content.Shared.Inventory;

namespace Content.Server.Speech.EntitySystems;

public sealed class EmoteBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteBlockerComponent, BeforeEmoteEvent>(OnEmoteEvent);
        SubscribeLocalEvent<EmoteBlockerComponent, InventoryRelayedEvent<BeforeEmoteEvent>>(OnRelayedEmoteEvent);
    }

    private static void OnRelayedEmoteEvent(Entity<EmoteBlockerComponent> entity, ref InventoryRelayedEvent<BeforeEmoteEvent> args)
    {
        OnEmoteEvent(entity, ref args.Args);
    }

    private static void OnEmoteEvent(Entity<EmoteBlockerComponent> entity, ref BeforeEmoteEvent args)
    {
        if (entity.Comp.BlocksEmotes.Contains(args.Emote))
        {
            args.Cancel();
            args.Blocker = entity;
            return;
        }

        foreach (var blockedCat in entity.Comp.BlocksCategories)
        {
            if (blockedCat == args.Emote.Category)
            {
                args.Cancel();
                args.Blocker = entity;
                return;
            }
        }
    }
}
