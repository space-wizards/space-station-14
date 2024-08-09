namespace Content.Server.NPC.Systems;

/// <summary>
/// Stores all of the global knowledge data for NPCs to retrieve as needed.
/// </summary>
[RegisterComponent]
public sealed partial class NpcWorldComponent : Component
{
    /*
     * Stims that are currently active for NPCs to pull from.
     * We do this in the parallel updates rather than when raising eventbus events just because it's easy to do it in
     * parallel here.
     */
}
