namespace Content.Shared.Weapons.Ranged.Components;

public abstract class BatteryAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// How much battery it costs to fire once.
    /// </summary>
    [DataField("fireCost")]
    [AutoNetworkedField]
    public float FireCost = 100;

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!
    [AutoNetworkedField]
    [ViewVariables]
    public int Shots;

    [AutoNetworkedField]
    [ViewVariables]
    public int Capacity;
}
