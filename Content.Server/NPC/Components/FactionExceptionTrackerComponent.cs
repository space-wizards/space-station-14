using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;

/// <summary>
/// This is used for tracking entities stored in <see cref="FactionExceptionComponent"/>
/// </summary>
[RegisterComponent, Access(typeof(NpcFactionSystem))]
public sealed partial class FactionExceptionTrackerComponent : Component
{
    /// <summary>
    /// entities with <see cref="FactionExceptionComponent"/> that are tracking this entity.
    /// </summary>
    [DataField("entities")]
    public HashSet<EntityUid> Entities = new();
}
