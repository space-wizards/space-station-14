namespace Content.Shared.Inventory.ShitRockable.Components;

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
