using Content.Server.Chat.Systems;
using Content.Server.Dragon;
using Content.Server.GameTicking.Rules;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Store.Components;
using Content.Server.Revolutionary.Components;
using Content.Server.Store.Systems;
using Content.Shared.Dragon;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Store;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Revolutionary;

/// <summary>
/// Handles the revolutionary supply rift system.
/// </summary>
public sealed class RevSupplyRiftSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DragonRiftSystem _dragonRift = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private const string RevSupplyRiftListingId = "RevSupplyRiftListing";
    private readonly Dictionary<EntityUid, ListingCondition> _riftConditions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonRiftComponent, ComponentStartup>(OnRiftStartup);
        SubscribeLocalEvent<DragonRiftComponent, ComponentShutdown>(OnRiftShutdown);
        SubscribeLocalEvent<RevSupplyRiftComponent, ComponentStartup>(OnRevRiftStartup);
    }

    private void OnRiftStartup(EntityUid uid, DragonRiftComponent component, ComponentStartup args)
    {
        // Only apply to supply rifts
        if (!HasComp<RevSupplyRiftComponent>(uid))
            return;

        // Add the RevSupplyRiftComponent if it doesn't exist
        var revRift = EnsureComp<RevSupplyRiftComponent>(uid);
        revRift.PlacedTime = _timing.CurTime;
        revRift.State = DragonRiftState.Charging;

        // Disable the supply rift listing for all revolutionaries
        DisableSupplyRiftListing();

        // Send a message to all revolutionaries about the rift
        SendRiftPlacedMessage(uid);
    }

    private void OnRevRiftStartup(EntityUid uid, RevSupplyRiftComponent component, ComponentStartup args)
    {
        // This is called when a RevSupplyRiftComponent is added to an entity
        // We need to make sure the DragonRiftComponent is also present
        if (!TryComp<DragonRiftComponent>(uid, out var dragonRift))
            return;

        component.PlacedTime = _timing.CurTime;
        component.State = dragonRift.State;
    }

    private void OnRiftShutdown(EntityUid uid, DragonRiftComponent component, ComponentShutdown args)
    {
        // Only apply to supply rifts
        if (!HasComp<RevSupplyRiftComponent>(uid))
            return;

        // Re-enable the supply rift listing for all revolutionaries
        EnableSupplyRiftListing();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for rifts that have finished charging
        var query = EntityQueryEnumerator<RevSupplyRiftComponent, DragonRiftComponent>();
        while (query.MoveNext(out var uid, out var revRift, out var dragonRift))
        {
            // Update the state from the dragon rift component
            if (revRift.State != dragonRift.State)
            {
                revRift.State = dragonRift.State;

                // If the rift is now finished, enable the supply rift listing
                if (revRift.State == DragonRiftState.Finished)
                {
                    EnableSupplyRiftListing();
                }
            }
        }
    }

    /// <summary>
    /// Disables the supply rift listing in all revolutionary uplinks.
    /// </summary>
    private void DisableSupplyRiftListing()
    {
        // Create a condition that always returns false
        var condition = new RevSupplyRiftDisabledCondition();

        // Find all store components
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var store))
        {
            // Find the supply rift listing
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID == RevSupplyRiftListingId)
                {
                    // Add our condition to disable the listing
                    if (listing.Conditions == null)
                        listing.Conditions = new List<ListingCondition>();

                    _riftConditions[uid] = condition;
                    listing.Conditions.Add(condition);
                    break;
                }
            }
        }

        Logger.InfoS("rev-supply-rift", "Disabled supply rift listing in all uplinks");
    }

    /// <summary>
    /// Enables the supply rift listing in all revolutionary uplinks.
    /// </summary>
    private void EnableSupplyRiftListing()
    {
        // Find all store components
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var store))
        {
            // Find the supply rift listing
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID == RevSupplyRiftListingId && listing.Conditions != null)
                {
                    // Remove our condition to enable the listing
                    if (_riftConditions.TryGetValue(uid, out var condition))
                    {
                        listing.Conditions.Remove(condition);
                        _riftConditions.Remove(uid);
                    }
                    break;
                }
            }

            // Update the UI to reflect the changes
            _store.UpdateUserInterface(null, uid, store);
        }

        Logger.InfoS("rev-supply-rift", "Enabled supply rift listing in all uplinks");
    }

    /// <summary>
    /// Sends a message to all revolutionaries about the rift being placed.
    /// </summary>
    private void SendRiftPlacedMessage(EntityUid riftUid)
    {
        if (!TryComp<TransformComponent>(riftUid, out var xform))
            return;

        // Get the nearest beacon location
        var locationString = _navMap.GetNearestBeaconString((riftUid, xform));
        var message = Loc.GetString("rev-supply-rift-placed", ("location", locationString));

        // Find all revolutionaries and head revolutionaries
        var query = EntityQueryEnumerator<MindContainerComponent>();
        while (query.MoveNext(out var uid, out var mindContainer))
        {
            if (!mindContainer.HasMind || !_mind.TryGetMind(uid, out var mindId, out var mind))
                continue;

            // Check if the entity is a revolutionary or head revolutionary
            if (HasComp<RevolutionaryComponent>(uid) || HasComp<HeadRevolutionaryComponent>(uid))
            {
                // Send a private message to the revolutionary
                _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Whisper, false, true);
            }
        }

        Logger.InfoS("rev-supply-rift", $"Sent rift placed message to all revolutionaries: {message}");
    }
}

/// <summary>
/// A condition that always returns false, used to disable the supply rift listing.
/// </summary>
public sealed class RevSupplyRiftDisabledCondition : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        return false;
    }
}
