using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Configures this specific closet for random closing
/// </summary>
[RegisterComponent]
public sealed partial class HauntedClosetComponent : Component
{
    /// <summary>
    /// Higher means it will be selected more often by the rule
    /// </summary>
    [DataField]
    public float CloseChance = 1f;

    /// <summary>
    /// Loot that will be placed
    /// </summary>
    [DataField]
    public EntityTableSelector? FillLoot = null;

    /// <summary>
    /// There needs to be at most FillEntityMax entities in the container for it to be refilled when closing
    /// </summary>
    [DataField]
    public int FillEntityMax = 2;
}
