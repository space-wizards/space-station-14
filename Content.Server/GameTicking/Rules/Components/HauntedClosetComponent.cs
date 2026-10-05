using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Configures this specific closet for random closing
/// </summary>
[RegisterComponent]
public sealed partial class HauntedClosetComponent : Component
{
    /// <summary>
    /// 1 = always closed, 0 = never closed
    /// </summary>
    [DataField]
    public float CloseChance = 0.01f;

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
