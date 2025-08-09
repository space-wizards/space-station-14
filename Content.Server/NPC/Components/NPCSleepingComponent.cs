namespace Content.Server.NPC.Components;

/// <summary>
/// Added to NPCs which are currently sleeping.
/// </summary>
[RegisterComponent]
public sealed partial class NPCSleepingComponent : Component
{
    /// <summary>
    /// A hashset of different references, this allows you to sleep NPCs from differen systems withou worring about
    /// overriding eachother. E.g. one system wants to sleep an entity because its dead, another because its not nearby
    /// anyone - you wouldn't want those two to conflict with eachother.
    /// </summary>
    [DataField]
    public HashSet<NPCSleepingCategories> SleepReferences = new();
}

public enum NPCSleepingCategories : byte
{
    Default,
    PlayerAttach,
    MobState,
    ProxySleep,
}
