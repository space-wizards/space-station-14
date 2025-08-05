using Content.Server.Administration.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Dragon;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Chat;
using Content.Shared.Dragon;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Store;
using Content.Shared.Store.Events;
using Robust.Shared.Maths;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Server.Revolutionary;

/// <summary>
/// Handles the revolutionary supply rift system.
/// </summary>
public sealed class RevSupplyRiftSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly Chat.Managers.IChatManager _chatManager = default!;
    [Dependency] private readonly DragonRiftSystem _dragonRift = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    private const string RevSupplyRiftListingId = "RevSupplyRiftListing";
    
    /// <summary>
    /// The current active rift entity, if any.
    /// </summary>
    private EntityUid? _activeRift = null;
    
    /// <summary>
    /// Dictionary to track the original descriptions of listings.
    /// </summary>
    private readonly Dictionary<EntityUid, string> _originalDescriptions = new();
    
    /// <summary>
    /// Tracks whether a rift has been destroyed.
    /// </summary>
    private bool _riftDestroyed = false;
    
    /// <summary>
    /// Flag to track if a rift purchase is currently being processed.
    /// This prevents multiple rifts from being purchased by spam-clicking.
    /// </summary>
    private bool _isProcessingRift = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonRiftComponent, ComponentStartup>(OnRiftStartup);
        SubscribeLocalEvent<RevSupplyRiftComponent, ComponentStartup>(OnRevRiftStartup);
        SubscribeLocalEvent<RevSupplyRiftComponent, ComponentShutdown>(OnRevRiftShutdown);
        SubscribeLocalEvent<StorePurchaseAttemptEvent>(OnStorePurchaseAttempt);
        SubscribeLocalEvent<StorePurchaseCompletedEvent>(OnStorePurchaseCompleted);
        
        // Subscribe to the round restart cleanup event to reset the rift destroyed flag
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }
    
    /// <summary>
    /// Handles the RoundRestartCleanupEvent to reset the rift destroyed flag.
    /// This ensures that revolutionaries can place rifts in the new round.
    /// </summary>
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        // Reset the rift destroyed flag
        _riftDestroyed = false;
        _activeRift = null;
        _isProcessingRift = false;
        
        // Clear the original descriptions dictionary
        _originalDescriptions.Clear();
    }
    
    /// <summary>
    /// Checks if a rift purchase is currently being processed.
    /// </summary>
    /// <returns>True if a rift is being processed, false otherwise.</returns>
    public bool IsRiftBeingProcessed()
    {
        // Return true if either a rift is currently being processed or there's already an active rift
        return _isProcessingRift || _activeRift != null;
    }
    
    /// <summary>
    /// Sets the rift processing flag.
    /// </summary>
    /// <param name="isProcessing">Whether a rift is being processed.</param>
    public void SetRiftProcessing(bool isProcessing)
    {
        _isProcessingRift = isProcessing;
    }
    
    /// <summary>
    /// Handles the StorePurchaseAttemptEvent for revolutionary supply rifts.
    /// </summary>
    private void OnStorePurchaseAttempt(ref StorePurchaseAttemptEvent args)
    {
        // Only handle the revolutionary supply rift listing
        if (args.ListingId != RevSupplyRiftListingId)
            return;
        
        // Check if a rift has been destroyed
        if (_riftDestroyed)
        {
            // A rift has been destroyed, so cancel this purchase
            args.Cancel = true;
            return;
        }
        
        // Check if there's already an active rift being processed or placed
        if (IsRiftBeingProcessed())
        {
            // A rift is already being processed, so cancel this purchase
            args.Cancel = true;
            return;
        }
        
        // Mark that we're processing a rift purchase
        SetRiftProcessing(true);
    }
    
    /// <summary>
    /// Handles the StorePurchaseCompletedEvent for revolutionary supply rifts.
    /// </summary>
    private void OnStorePurchaseCompleted(ref StorePurchaseCompletedEvent args)
    {
        // Only handle the revolutionary supply rift listing
        if (args.ListingId != RevSupplyRiftListingId)
            return;
        
        // Mark that we're done processing a rift purchase
        SetRiftProcessing(false);
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
        revRift.ChargePercentage = 0;
        
        // Store the active rift
        _activeRift = uid;
        
        // Try to get the name of the revolutionary who placed the rift
        // The Dragon property in DragonRiftComponent is actually the revolutionary player entity
        var revolutionary = component.Dragon;
        Logger.InfoS("rev-supply-rift", $"Revolutionary entity: {revolutionary}");
        
        if (revolutionary != null)
        {
            // Use the Identity system to get the player's name
            // This is more reliable than trying to get it from the mind component
            var name = Identity.Name(revolutionary.Value, EntityManager);
            Logger.InfoS("rev-supply-rift", $"Got name from Identity system: {name}");
            
            if (!string.IsNullOrEmpty(name))
            {
                revRift.PlacedBy = name;
                Logger.InfoS("rev-supply-rift", $"Set PlacedBy to: {revRift.PlacedBy}");
            }
            else
            {
                // Fall back to metadata if Identity system doesn't have a name
                if (TryComp<MetaDataComponent>(revolutionary.Value, out var metadata))
                {
                    revRift.PlacedBy = metadata.EntityName;
                    Logger.InfoS("rev-supply-rift", $"Set PlacedBy to metadata name: {revRift.PlacedBy}");
                }
                else
                {
                    revRift.PlacedBy = "Unknown";
                    Logger.InfoS("rev-supply-rift", "Set PlacedBy to Unknown (no metadata)");
                }
            }
        }
        else
        {
            // Try to find a nearby humanoid entity to use as the placer
            if (TryComp<TransformComponent>(uid, out var riftTransform))
            {
                // Get all entities with HumanoidAppearanceComponent within a small radius
                var nearbyHumanoids = EntityManager.EntityQuery<Content.Shared.Humanoid.HumanoidAppearanceComponent, TransformComponent>()
                    .Where(pair => 
                    {
                        var (_, otherTransform) = pair;
                        return riftTransform.MapID == otherTransform.MapID && 
                               (riftTransform.WorldPosition - otherTransform.WorldPosition).LengthSquared() < 4; // 2 unit radius
                    })
                    .Select(pair => pair.Item1.Owner)
                    .ToList();
                
                if (nearbyHumanoids.Count > 0)
                {
                    // Use the first nearby humanoid
                    var humanoid = nearbyHumanoids[0];
                    var name = Identity.Name(humanoid, EntityManager);
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        revRift.PlacedBy = name;
                        Logger.InfoS("rev-supply-rift", $"Revolutionary entity is null, using nearby humanoid: {name}");
                    }
                    else
                    {
                        revRift.PlacedBy = "Unknown";
                        Logger.InfoS("rev-supply-rift", "Revolutionary entity is null, nearby humanoid has no name");
                    }
                }
                else
                {
                    revRift.PlacedBy = "Unknown";
                    Logger.InfoS("rev-supply-rift", "Revolutionary entity is null, no nearby humanoids found");
                }
            }
            else
            {
                revRift.PlacedBy = "Unknown";
                Logger.InfoS("rev-supply-rift", "Revolutionary entity is null, set PlacedBy to Unknown");
            }
        }

        // Update the supply rift listing for all revolutionaries
        UpdateSupplyRiftListing();

        // Send a message to all revolutionaries about the rift
        SendRiftPlacedMessage(uid);
        
        // Play the soviet choir sound in a 5-tile radius around the rift
        if (TryComp<TransformComponent>(uid, out var transform))
        {
            var soundPath = new SoundPathSpecifier("/Audio/_Starlight/Effects/sov_choir.ogg");
            _audio.PlayPvs(soundPath, uid, AudioParams.Default.WithMaxDistance(5f).WithVolume(10f));
        }
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

    private void OnRevRiftShutdown(EntityUid uid, RevSupplyRiftComponent component, ComponentShutdown args)
    {
        // Only apply to supply rifts
        if (!HasComp<RevSupplyRiftComponent>(uid))
            return;

        // Check if this is a rift that was destroyed (not just a normal shutdown)
        if (TryComp<DragonRiftComponent>(uid, out var dragonRift) && 
            dragonRift.State != DragonRiftState.Finished && 
            _activeRift == uid)
        {
            // Mark that a rift has been destroyed
            _riftDestroyed = true;
            _activeRift = null;
            
            // Update all uplinks with the destroyed message
            UpdateRiftDestroyedListing();
            
            Logger.InfoS("rev-supply-rift", "A supply rift was destroyed!");
        }
        else
        {
            // Normal shutdown (e.g., rift finished charging)
            // Re-enable the supply rift listing for all revolutionaries if no rift has been destroyed
            if (!_riftDestroyed)
            {
                EnableSupplyRiftListing();
            }
        }
    }
    
    /// <summary>
    /// Updates all revolutionary uplinks with the rift destroyed message.
    /// </summary>
    private void UpdateRiftDestroyedListing()
    {
        // Find all store components
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var store))
        {
            // Find the supply rift listing
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID == RevSupplyRiftListingId)
                {
                    // Store the original description if we haven't already
                    if (!_originalDescriptions.TryGetValue(uid, out _))
                    {
                        _originalDescriptions[uid] = listing.Description ?? "";
                    }
                    
                    // Update the description with the destroyed message
                    listing.Description = Loc.GetString("rev-supply-rift-destroyed");
                    
                    // Disable the listing permanently
                    listing.Unavailable = true;
                    
                    break;
                }
            }
            
            // Update the UI to reflect the changes
            _store.UpdateUserInterface(null, uid, store);
        }

        Logger.InfoS("rev-supply-rift", "Updated all uplinks with rift destroyed message");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for rifts that have finished charging
        var query = EntityQueryEnumerator<RevSupplyRiftComponent, DragonRiftComponent>();
        while (query.MoveNext(out var uid, out var revRift, out var dragonRift))
        {
            // Calculate the charge percentage
            var percentage = (int)MathF.Round(dragonRift.Accumulator / dragonRift.MaxAccumulator * 100);
            
            // Update the charge percentage if it changed
            if (revRift.ChargePercentage != percentage)
            {
                revRift.ChargePercentage = percentage;
                
                // Update the listing description
                if (_activeRift == uid)
                {
                    UpdateSupplyRiftListing();
                }
            }
            
            // Update the state from the dragon rift component
            if (revRift.State != dragonRift.State)
            {
                revRift.State = dragonRift.State;

                // If the rift is now finished, update the listing with the charged description
                if (revRift.State == DragonRiftState.Finished)
                {
                    _activeRift = null;
                    UpdateChargedRiftListing();
                }
            }
        }
    }

    /// <summary>
    /// Updates the supply rift listing in all revolutionary uplinks with the current charging status.
    /// </summary>
    private void UpdateSupplyRiftListing()
    {
        if (_activeRift == null || !TryComp<RevSupplyRiftComponent>(_activeRift.Value, out var revRift))
            return;
        
        // Get the location of the rift
        string locationString = "unknown location";
        if (TryComp<TransformComponent>(_activeRift.Value, out var xform))
        {
            locationString = _navMap.GetNearestBeaconString((_activeRift.Value, xform));
        }
        
        // Find all store components
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var store))
        {
            // Find the supply rift listing
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID == RevSupplyRiftListingId)
                {
                    // Store the original description if we haven't already
                    if (!_originalDescriptions.TryGetValue(uid, out _))
                    {
                        _originalDescriptions[uid] = listing.Description ?? "";
                    }
                    
                    // Update the description with the charging status and location
                    // Don't use color tags as they're not properly handled in the UI
                    var chargingText = $"Supply rift (Charging: {revRift.ChargePercentage}% - Placed by comrade {revRift.PlacedBy ?? "Unknown"} {locationString})";
                    
                    listing.Description = chargingText;
                    
                    // Disable the listing while a rift is charging
                    listing.Unavailable = true;
                    
                    break;
                }
            }
            
            // Update the UI to reflect the changes
            _store.UpdateUserInterface(null, uid, store);
        }
    }

    /// <summary>
    /// Disables the supply rift listing in all revolutionary uplinks.
    /// </summary>
    private void DisableSupplyRiftListing()
    {
        // Find all store components
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var store))
        {
            // Find the supply rift listing
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID == RevSupplyRiftListingId)
                {
                    // Store the original description if we haven't already
                    if (!_originalDescriptions.TryGetValue(uid, out _))
                    {
                        _originalDescriptions[uid] = listing.Description ?? "";
                    }
                    
                    // Disable the listing
                    listing.Unavailable = true;
                    
                    break;
                }
            }
            
            // Update the UI to reflect the changes
            _store.UpdateUserInterface(null, uid, store);
        }

        Logger.InfoS("rev-supply-rift", "Disabled supply rift listing in all uplinks");
    }

    /// <summary>
    /// Updates all revolutionary uplinks with the charged rift description and active rift count.
    /// </summary>
    private void UpdateChargedRiftListing()
    {
        // Count active rifts (those that are in Finished state)
        int activeRiftCount = 0;
        var riftsQuery = EntityQueryEnumerator<RevSupplyRiftComponent, DragonRiftComponent>();
        while (riftsQuery.MoveNext(out _, out var revRift, out var dragonRift))
        {
            if (revRift.State == DragonRiftState.Finished)
            {
                activeRiftCount++;
            }
        }
        
        // Find all store components
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var store))
        {
            // Find the supply rift listing
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID == RevSupplyRiftListingId)
                {
                    // Update the description with the charged message and active rift count
                    listing.Description = Loc.GetString("rev-supply-rift-charged", ("count", activeRiftCount));
                    
                    // Enable the listing since the rift is charged
                    listing.Unavailable = false;
                    
                    break;
                }
            }
            
            // Update the UI to reflect the changes
            _store.UpdateUserInterface(null, uid, store);
        }

        Logger.InfoS("rev-supply-rift", $"Updated all uplinks with charged rift description. Active rifts: {activeRiftCount}");
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
                if (listing.ID == RevSupplyRiftListingId)
                {
                    // Restore the original description if we have it
                    if (_originalDescriptions.TryGetValue(uid, out var originalDesc))
                    {
                        listing.Description = originalDesc;
                    }
                    // Don't set a new description if we don't have the original
                    // This will use the default description from the prototype
                    
                    // Enable the listing
                    listing.Unavailable = false;
                    
                    break;
                }
            }

            // Update the UI to reflect the changes
            _store.UpdateUserInterface(null, uid, store);
        }

        Logger.InfoS("rev-supply-rift", "Enabled supply rift listing in all uplinks");
    }
    
    /// <summary>
    /// Checks if a rift has been destroyed and updates the listing accordingly.
    /// This is called by the StoreSystem whenever listings are refreshed.
    /// </summary>
    /// <param name="storeComp">The store component being refreshed</param>
    public void CheckRiftDestroyedAndUpdateListing(StoreComponent storeComp)
    {
        // If no rift has been destroyed, we don't need to do anything
        if (!_riftDestroyed)
            return;
            
        // Find the supply rift listing
        foreach (var listing in storeComp.FullListingsCatalog)
        {
            if (listing.ID == RevSupplyRiftListingId)
            {
                // Store the original description if we haven't already
                if (!_originalDescriptions.TryGetValue(storeComp.Owner, out _))
                {
                    _originalDescriptions[storeComp.Owner] = listing.Description ?? "";
                }
                
                // Update the description with the destroyed message
                listing.Description = Loc.GetString("rev-supply-rift-destroyed");
                
                // Disable the listing permanently
                listing.Unavailable = true;
                
                break;
            }
        }
    }

    /// <summary>
    /// Sends a message to all revolutionaries about the rift being placed. And also ghosts
    /// </summary>
    private void SendRiftPlacedMessage(EntityUid riftUid)
    {
        if (!TryComp<TransformComponent>(riftUid, out var xform) || 
            !TryComp<RevSupplyRiftComponent>(riftUid, out var revRift))
            return;

        // Get the nearest beacon location
        var locationString = _navMap.GetNearestBeaconString((riftUid, xform));
        var placedBy = revRift.PlacedBy ?? "Unknown";
        var message = Loc.GetString("rev-supply-rift-placed", ("location", locationString), ("name", placedBy));
        var sender = Loc.GetString("rev-supply-rift-sender");

        // Find all revolutionaries and head revolutionaries
        var query = EntityQueryEnumerator<MindContainerComponent>();
        while (query.MoveNext(out var uid, out var mindContainer))
        {
            if (!mindContainer.HasMind || mindContainer.Mind == null)
                continue;

            // Get the mind component
            var mindComp = Comp<MindComponent>(mindContainer.Mind.Value);

            // Check if the entity is a revolutionary or head revolutionary
            if (HasComp<RevolutionaryComponent>(uid) || HasComp<HeadRevolutionaryComponent>(uid))
            {
                // Send a private message to the revolutionary
                var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", 
                    ("sender", sender), 
                    ("message", message));
                
                // Only send if the player is connected
                if (mindComp.UserId != null && _playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
                {
                    // Use red color for the message
                    var color = Color.Red;
                    _chatManager.ChatMessageToOne(ChatChannel.Radio, message, wrappedMessage, uid, false, session.Channel, color);
                }
            }
        }

        // Send to ghosts so they don't miss out
        var nonAdminGhostClients = Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Where(p => !_adminManager.IsAdmin(p))
            .Select(p => p.Channel);
        
        var ghostWrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", 
            ("sender", sender), 
            ("message", message));
        
        _chatManager.ChatMessageToMany(ChatChannel.Dead, message, ghostWrappedMessage, riftUid, false, true, nonAdminGhostClients.ToList(), Color.Red);

        // Send alert
        var adminMessage = $"Revolutionary supply rift placed by {placedBy} {locationString}";
        _chatManager.SendAdminAlert(adminMessage);
        
        // And announcement
        var adminClients = _adminManager.ActiveAdmins.Select(p => p.Channel);
        var adminWrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", 
            ("sender", sender), 
            ("message", message));
        
        _chatManager.ChatMessageToMany(ChatChannel.Admin, message, adminWrappedMessage, riftUid, false, true, adminClients.ToList(), Color.Red);
    }

    /// <summary>
    /// Gets all clients
    /// </summary>
    private IEnumerable<INetChannel> GetDeadChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }
}
