using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Dice;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedDiceSystem))]
public sealed partial class DiceComponent : Component
{
    [DataField("sound")]
    public SoundSpecifier Sound { get; private set; } = new SoundCollectionSpecifier("Dice");

    /// <summary>
    ///     Multiplier for the value  of a die. Applied after the <see cref="Offset"/>.
    /// </summary>
    [DataField("multiplier")]
    public int Multiplier { get; private set; } = 1;

    /// <summary>
    ///     Quantity that is subtracted from the value of a die. Can be used to make dice that start at "0". Applied
    ///     before the <see cref="Multiplier"/>
    /// </summary>
    [DataField("offset")]
    public int Offset { get; private set; } = 0;

    [DataField("sides")]
    public int Sides { get; private set; } = 20;

    /// <summary>
    ///     The currently displayed value.
    /// </summary>
    [DataField("currentValue")]
    public int CurrentValue { get; set; } = 20;

    [Serializable, NetSerializable]
    public sealed class DiceState : ComponentState
    {
        public readonly int CurrentValue;
        public DiceState(int value)
        {
            CurrentValue = value;
        }
    }
}
