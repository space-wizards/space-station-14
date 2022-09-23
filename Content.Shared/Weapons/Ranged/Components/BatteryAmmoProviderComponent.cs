namespace Content.Shared.Weapons.Ranged.Components;

public abstract class BatteryAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// How much battery it costs to fire once.
    /// </summary>
    [ViewVariables, DataField("fireCost")]
    public float FireCost = 100;

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!

    [ViewVariables]
    public int Shots;

    [ViewVariables]
    public int Capacity;

    /// <summary>
    ///     The examine group used for grouping together examine details.
    /// </summary>
    [DataField("examineGroup")]
    public string ExamineGroup = "gun";

    [DataField("examinePriority")]
    public int ExaminePriority = 20;
}
