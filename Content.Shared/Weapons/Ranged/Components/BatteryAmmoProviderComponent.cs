namespace Content.Shared.Weapons.Ranged.Components;

public abstract partial class BatteryAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// How much battery it costs to fire once.
    /// </summary>
    [DataField("fireCost")]
    public float FireCost = 100;

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!

    [ViewVariables]
    public int Shots;

    [ViewVariables]
    public int Capacity;
}
