namespace Content.Shared.Weapons.Ranged;

public abstract class BatteryAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// How much battery it costs to fire once.
    /// </summary>
    [ViewVariables, DataField("fireCost")]
    public float FireCost;

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!

    [ViewVariables]
    public int Shots;
}
