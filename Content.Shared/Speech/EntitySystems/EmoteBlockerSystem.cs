using Content.Shared.Chat;
using Content.Shared.Cuffs;
using Content.Shared.Inventory;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class EmoteBlockerSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private static void OnRelayedEmoteEvent(Entity<EmoteBlockerComponent> ent, ref InventoryRelayedEvent<BeforeEmoteEvent> args)
    {
        OnEmoteEvent(ent, ref args.Args);
    }

    [SubscribeLocalEvent]
    private static void OnCuffedRelayEvent(Entity<EmoteBlockerComponent> ent, ref CuffedRelayEvent<BeforeEmoteEvent> args)
    {
        OnEmoteEvent(ent, ref args.Args);
    }

    [SubscribeLocalEvent]
    private static void OnEmoteEvent(Entity<EmoteBlockerComponent> ent, ref BeforeEmoteEvent args)
    {
        if (ent.Comp.BlocksEmotes.Contains(args.Emote))
        {
            args.Cancel();
            args.Blocker = ent;
            return;
        }

        foreach (var blockedCat in ent.Comp.BlocksCategories)
        {
            if (blockedCat == args.Emote.Category)
            {
                args.Cancel();
                args.Blocker = ent;
                return;
            }
        }
    }
}
