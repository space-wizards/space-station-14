using System.ComponentModel;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;

namespace Content.Server.Speech;

public sealed class EmoteBlockerSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        // "Manually" relay the event to the inventory to avoid needing to put EmoteEvent in Content.Shared .
        SubscribeLocalEvent<InventoryComponent, EmoteEvent>(RelayEvent, before: [typeof(VocalSystem)]);

        SubscribeLocalEvent<EmoteBlockerComponent, EmoteEvent>(OnEmoteEvent, before: [typeof(VocalSystem)]);
        SubscribeLocalEvent<EmoteBlockerComponent, InventoryRelayedEvent<EmoteEvent>>(OnRelayedEmoteEvent, before: [typeof(VocalSystem)]);
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
