namespace Content.Shared.Inventory.ShitRockable.Components;

/// <summary>
/// Items with this component will recieve damage and may be thrown with prob Chance when their wearer is hit with damage according to <see cref="ShitRockableComponent">.
/// </summary>
[RegisterComponent]
public sealed partial class RockableItemComponent : Component
{
    /// <summary>
    /// The chance for the item to be rocked from its inventory slot when enough damage is recieved <see cref="ShitRockableComponent">.
    /// </summary>
    [DataField]
    public float Chance = .125f; // 1:8

    /// <summary>
    /// Should the item be able to be knocked off? If true the item is still damaged.
    /// </summary>
    [DataField]
    public bool DontThrow = false;
}
