using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Dice;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedDiceSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class DiceComponent : Component
{
    /// <summary>
    /// The sounds to play when rolling the die.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound { get; private set; } = new SoundCollectionSpecifier("Dice");

    /// <summary>
    /// Multiplier for the value of a die. Applied after the <see cref="Offset"/>.
    /// </summary>
    [DataField]
    public int Multiplier { get; private set; } = 1;

    /// <summary>
    /// Quantity that is subtracted from the value of a die. Can be used to make dice that start at "0". Applied
    /// before the <see cref="Multiplier"/>
    /// </summary>
    [DataField]
    public int Offset { get; private set; } = 0;

    /// <summary>
    /// The number of sides the dice has.
    /// </summary>
    [DataField]
    public int Sides { get; private set; } = 20;

    /// <summary>
    /// A localized string of the type of object type this is (a die? a coin? a magic pool ball?), with how many sides.
    /// This gets inserted in <c>$name</c> in <c>dice-component-on-examine-message-part-1</c>. If null, the examine string will be omitted.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public LocId? ExamineObjectText = "dice-component-type-die";

    /// <summary>
    /// A localized string of how to print the value this has landed on when examining the entity.
    /// Expects the die's value at <c>$currentSide</c>.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public LocId ExamineLandedOnText = "dice-component-roll-generic";

    /// <summary>
    /// A dataset of the values. Expected to contain values from 1 to Sides.
    /// If null, a numeric value will be printed out instead.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<LocalizedDatasetPrototype>? Values;

    /// <summary>
    /// An optional value for this die to land on if weighted.
    /// </summary>
    [DataField]
    public int? WeightedValue = null;

    /// <summary>
    /// If <see cref="WeightedValue"/> is not null, the die will roll that value with this probability, otherwise it selects a random value.
    /// </summary>
    [DataField]
    public float WeightedProb = 1.0f;

    /// <summary>
    /// The currently displayed value.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentValue = 20;
}
