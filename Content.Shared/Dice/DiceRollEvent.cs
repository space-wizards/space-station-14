namespace Content.Shared.Dice;

/// <summary>
///     An event raised whenever a die is being rolled, to allow for the final value to be influenced.
/// </summary>
[ByRefEvent]
public struct DiceRollEvent
{
    /// <summary>
    ///     The entity which rolled the die.
    ///     May be null if, for example, an explosion sent a die flying.
    /// </summary>
    public readonly EntityUid? Roller;

    /// <summary>
    ///     The value of the roll before any modifiers.
    /// </summary>
    public readonly int NaturalRoll;

    /// <summary>
    ///     The value of the roll after any modifiers or effects are applied.
    ///     Subscribers to this event can freely modify this value.
    /// </summary>
    public int Roll;

    public DiceRollEvent(int naturalRoll, EntityUid? roller = null)
    {
        NaturalRoll = naturalRoll;
        Roll = naturalRoll;
        Roller = roller;
    }
}
