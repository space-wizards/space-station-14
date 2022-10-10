using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

public abstract class BatteryAmmoProviderComponent : AmmoProviderComponent, IExamineGroup
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

    [DataField("examineGroup", customTypeSerializer: typeof(PrototypeIdSerializer<ExamineGroupPrototype>))]
    public string ExamineGroup { get; set; } = "gun";

    [DataField("examinePriority")]
    public float ExaminePriority { get; set; } = 20;
}
