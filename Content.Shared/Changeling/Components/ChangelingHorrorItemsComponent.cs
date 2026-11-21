namespace Content.Shared.Changeling.Components;

/// <summary>
/// Stores the horror's inventory
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingHorrorItemsComponent : Component
{
    /// <summary>
    /// The items themselves
    /// </summary>
    public List<(EntityUid item, string slot)> LingSpaceItems;
}
