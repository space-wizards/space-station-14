using Content.Server.Speech.Components;
using Content.Shared.Emoting;
using Content.Shared.Inventory;

namespace Content.Server.Speech;

public sealed class EmoteBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteBlockerComponent, BeforeEmoteEvent>(OnEmoteEvent);
        SubscribeLocalEvent<EmoteBlockerComponent, InventoryRelayedEvent<BeforeEmoteEvent>>(OnRelayedEmoteEvent);
    }

    private void OnRelayedEmoteEvent(EntityUid uid, EmoteBlockerComponent component, InventoryRelayedEvent<BeforeEmoteEvent> args)
    {
        OnEmoteEvent(uid, component, ref args.Args);
    }

    private void OnEmoteEvent(EntityUid uid, EmoteBlockerComponent component, ref BeforeEmoteEvent args)
    {
        if (component.BlocksEmotes.Contains(args.Emote))
        {
            args.Cancel();
            args.Blocker = uid;
            return;
        }

        foreach (var blockedCat in component.BlocksCategories)
        {
            if (blockedCat == args.Emote.Category)
            {
                args.Cancel();
                args.Blocker = uid;
                return;
            }
        }
    }
}
