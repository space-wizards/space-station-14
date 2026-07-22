using Content.Shared.Inventory;
using Content.Shared.VentCrawl.Components;

namespace Content.Shared.VentCrawl;

/// <summary>
/// Handles vent-crawling ability granted directly or by equipped clothing.
/// </summary>
public sealed partial class VentCrawlerSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlerComponent, CanVentCrawlEvent>(OnCanVentCrawl);
        SubscribeLocalEvent<VentCrawlerComponent, InventoryRelayedEvent<CanVentCrawlEvent>>(OnCanVentCrawlInventory);
    }

    private void OnCanVentCrawl(Entity<VentCrawlerComponent> ent, ref CanVentCrawlEvent args)
    {
        args.Crawler = ent;
    }

    private void OnCanVentCrawlInventory(Entity<VentCrawlerComponent> ent, ref InventoryRelayedEvent<CanVentCrawlEvent> args)
    {
        args.Args.Crawler ??= ent;
    }

    /// <summary>
    /// Gets the vent-crawler data that applies to an entity.
    /// </summary>
    public bool TryGetVentCrawler(EntityUid uid, out Entity<VentCrawlerComponent> crawler)
    {
        var ev = new CanVentCrawlEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.Crawler is not { } found)
        {
            crawler = default;
            return false;
        }

        crawler = found;
        return true;
    }
}

/// <summary>
/// Raised on an entity to check whether it is capable of vent-crawling.
/// </summary>
[ByRefEvent]
public record struct CanVentCrawlEvent : IInventoryRelayEvent
{
    public Entity<VentCrawlerComponent>? Crawler;

    /// <inheritdoc/>
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
