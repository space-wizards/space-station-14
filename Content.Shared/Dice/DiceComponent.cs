using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Dice;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedDiceSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class DiceComponent : Component
{
    [DataField]
    public SoundSpecifier Sound { get; private set; } = new SoundCollectionSpecifier("Dice");

    /// <summary>
    /// Multiplier for the value  of a die. Applied after the <see cref="Offset"/>.
    /// </summary>
    [DataField]
    public int Multiplier { get; private set; } = 1;

    /// <summary>
    /// Quantity that is subtracted from the value of a die. Can be used to make dice that start at "0". Applied
    /// before the <see cref="Multiplier"/>
    /// </summary>
    [DataField]
    public int Offset { get; private set; } = 0;

    [DataField]
    public int Sides { get; private set; } = 20;

    /// <summary>
    /// A localized string of the type of object type this is (a die? a coin? a magic pool ball?)
    /// If null, the first part of the examine text will be omitted.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public LocId? ExamineObjectText = "dice-component-type-die";

    /// <summary>
    /// A localized string of how to print the value this has landed on.
    /// Expects its value at currentSide.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public LocId LandedString = "dice-component-roll-generic";

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
    /// If <c cref="WeightedValue"/> is not null, the die will roll that value with this probability, otherwise it selects a random value.
    /// </summary>
    [DataField]
    public float WeightedProb = 1.0f;

    /// <summary>
    ///     The currently displayed value.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentValue { get; set; } = 20;

}
