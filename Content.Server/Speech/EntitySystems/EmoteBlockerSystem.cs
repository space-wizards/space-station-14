using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Inventory;

namespace Content.Server.Speech;

public sealed class EmoteBlockerSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        // "Manually" relay the event to the inventory to avoid needing to put EmoteEvent in Content.Shared .
        SubscribeLocalEvent<InventoryComponent, EmoteEvent>(RelayEvent);

        SubscribeLocalEvent<EmoteBlockerComponent, EmoteEvent>(OnEmoteEvent);
        SubscribeLocalEvent<EmoteBlockerComponent, InventoryRelayedEvent<EmoteEvent>>(OnRelayedEmoteEvent);
    }

    private void RelayEvent(Entity<InventoryComponent> entity, ref EmoteEvent args)
    {
        _inventory.RelayEvent(entity, ref args);
    }

    private void OnRelayedEmoteEvent(EntityUid uid, EmoteBlockerComponent component, InventoryRelayedEvent<EmoteEvent> args)
    {
        OnEmoteEvent(uid, component, ref args.Args);
    }

    private void OnEmoteEvent(EntityUid uid, EmoteBlockerComponent component, ref EmoteEvent args)
    {
        if (component.BlocksEmotes.Contains(args.Emote))
        {
            args.Blocked = true;
            return;
        }

        foreach (var blockedCat in component.BlocksCategories)
        {
            if (blockedCat == args.Emote.Category)
            {
                args.Blocked = true;
                return;
            }
        }
    }
}
