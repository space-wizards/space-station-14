using Content.Shared.Whitelist;

namespace Content.Server.NPC.Components;

/// <summary>
/// Will put NPCs to sleep if there are no actors within a certain distance. Will save a lot of server resources for
/// NPCs that aren't close to anyone.
/// </summary>
[RegisterComponent]
public sealed partial class NPCProximitySleepComponent : Component
{
    /// <summary>
    /// Time of the last update.
    /// </summary>
    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Updates the status of if the NPC is asleep or awake at this interval.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Will unpause the NPC if any actor is within this distance.
    /// </summary>
    [DataField]
    public float UnpauseProximity = 25.0f;

    /// <summary>
    /// Entities that match the whitelist will be ignored for proximity calculations.
    /// </summary>
    public EntityWhitelist ProximityIgnore = new()
    {
        Components = [ "Ghost" ],
    };

    /// <summary>
    /// A blacklist for the whitelist... This is literally just for aghosts ;(
    /// </summary>
    public EntityWhitelist ProximityDontIgnore = new()
    {
        Tags = [ "AllowBiomeLoading" ],
    };
}
