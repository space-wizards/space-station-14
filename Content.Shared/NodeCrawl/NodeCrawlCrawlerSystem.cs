using Content.Shared.Inventory;

namespace Content.Shared.NodeCrawl;

/// <summary>
/// Resolves which <see cref="NodeCrawlerComponent"/> applies to an entity,
/// supporting inventory-relayed crawlers.
/// </summary>
public sealed partial class NodeCrawlCrawlerSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnCanNodeCrawl(Entity<NodeCrawlerComponent> ent, ref CanNodeCrawlEvent args)
    {
        args.User = ent.Owner;
        args.Crawler = ent;
    }

    [SubscribeLocalEvent]
    private void OnCanNodeCrawlInventory(Entity<NodeCrawlerComponent> ent, ref InventoryRelayedEvent<CanNodeCrawlEvent> args)
    {
        if (!ent.Comp.Relay)
            return;

        args.Args.User = args.Owner;
        args.Args.Crawler = ent;
    }

    /// <summary>
    /// Checks if an entity has a node crawler.
    /// </summary>
    public bool HasNodeCrawler(EntityUid uid)
    {
        return TryGetNodeCrawler(uid, out _, out _);
    }

    public bool TryGetNodeCrawler(EntityUid uid, out Entity<NodeCrawlerComponent> crawler)
    {
        return TryGetNodeCrawler(uid, out crawler, out _);
    }

    /// <summary>
    /// Attempts to get the <see cref="NodeCrawlerComponent"/> that applies to an entity.
    /// </summary>
    public bool TryGetNodeCrawler(EntityUid uid, out Entity<NodeCrawlerComponent> crawler, out EntityUid user)
    {
        var ev = new CanNodeCrawlEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.User is not { } foundUser || ev.Crawler is not { } foundCrawler)
        {
            crawler = default;
            user = default!;
            return false;
        }

        user = foundUser;
        crawler = foundCrawler;
        return true;
    }
}

/// <summary>
/// Requests permission for an entity to begin node crawling.
/// </summary>
[ByRefEvent]
public record struct CanNodeCrawlEvent : IInventoryRelayEvent
{
    public EntityUid? User;
    public Entity<NodeCrawlerComponent>? Crawler;
    public SlotFlags TargetSlots => SlotFlags.INNERCLOTHING;
}
