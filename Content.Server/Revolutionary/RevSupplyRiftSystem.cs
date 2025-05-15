using Content.Server.Chat.Systems;
using Content.Server.Dragon;
using Content.Server.GameTicking.Rules;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Store.Components;
using Content.Server.Revolutionary.Components;
using Content.Server.Store.Systems;
using Content.Shared.Chat;
using Content.Shared.Dragon;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Store;
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

    private const string RevSupplyRiftListingId = "RevSupplyRiftListing";
    
    /// <summary>
    /// The current active rift entity, if any.
    /// </summary>
    private EntityUid? _activeRift = null;
    
    /// <summary>
    /// Dictionary to track the original descriptions of listings.
    /// </summary>
    private readonly Dictionary<EntityUid, string> _originalDescriptions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonRiftComponent, ComponentStartup>(OnRiftStartup);
        SubscribeLocalEvent<RevSupplyRiftComponent, ComponentStartup>(OnRevRiftStartup);
        SubscribeLocalEvent<RevSupplyRiftComponent, ComponentShutdown>(OnRevRiftShutdown);
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
        
        // Try to get the name of the player who placed the rift
        if (component.Dragon != null && TryComp<MetaDataComponent>(component.Dragon, out var metadata))
        {
            revRift.PlacedBy = metadata.EntityName;
        }
        else
        {
            revRift.PlacedBy = "Unknown";
        }

        // Update the supply rift listing for all revolutionaries
        UpdateSupplyRiftListing();

        // Send a message to all revolutionaries about the rift
        SendRiftPlacedMessage(uid);
        
        // Play the soviet choir sound in a 5-tile radius around the rift
        if (TryComp<TransformComponent>(uid, out var transform))
        {
            var soundPath = new SoundPathSpecifier("/Audio/_Starlight/Effects/sov_choir.ogg");
            _audio.PlayPvs(soundPath, uid, AudioParams.Default.WithMaxDistance(5f).WithVolume(0f));
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

                // If the rift is now finished, enable the supply rift listing
                if (revRift.State == DragonRiftState.Finished)
                {
                    _activeRift = null;
                    EnableSupplyRiftListing();
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
                    
                    // Update the description with the charging status
                    var chargingText = Loc.GetString("rev-supply-rift-charging", 
                        ("percentage", revRift.ChargePercentage), 
                        ("name", revRift.PlacedBy ?? "Unknown"));
                    
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
    /// Sends a message to all revolutionaries about the rift being placed.
    /// </summary>
    private void SendRiftPlacedMessage(EntityUid riftUid)
    {
        if (!TryComp<TransformComponent>(riftUid, out var xform))
            return;

        // Get the nearest beacon location
        var locationString = _navMap.GetNearestBeaconString((riftUid, xform));
        var message = Loc.GetString("rev-supply-rift-placed", ("location", locationString));
        var sender = Loc.GetString("rev-supply-rift-sender");

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
                var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", 
                    ("sender", sender), 
                    ("message", message));
                
                // Only send if the player is connected
                if (mind.Session != null)
                {
                    // Use red color for the message
                    var color = Color.Red;
                    _chatManager.ChatMessageToOne(ChatChannel.Radio, message, wrappedMessage, uid, false, mind.Session.Channel, color);
                }
            }
        }

        Logger.InfoS("rev-supply-rift", $"Sent rift placed message to all revolutionaries: {message}");
    }
}
