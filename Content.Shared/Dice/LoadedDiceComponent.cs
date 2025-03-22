using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Dice;

/// <summary>
///     A "loaded" die, which can be manually set to always roll a predetermined side.
///     Does nothing unless paired with a <see cref="DiceComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedLoadedDiceSystem))]
[AutoGenerateComponentState]
public sealed partial class LoadedDiceComponent : Component
{
    /// <summary>
    ///     The currently selected side. If null, the die behaves as normal.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public int? SelectedSide = null;

    /// <summary>
    ///     Current user holding the loaded die.
    /// </summary>
    [ViewVariables]
    public EntityUid? User;
}

[Serializable, NetSerializable]
public sealed class LoadedDiceBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly int Multiplier;
    public readonly int Offset;
    public readonly int Sides;

    public readonly int? SelectedSide;

    public LoadedDiceBoundUserInterfaceState(DiceComponent dice, int? selectedSide)
    {
        Multiplier = dice.Multiplier;
        Offset = dice.Offset;
        Sides = dice.Sides;

        SelectedSide = selectedSide;
    }
}

[Serializable, NetSerializable]
public sealed class LoadedDiceSideSelectedMessage : BoundUserInterfaceMessage
{
    public readonly int? SelectedSide;

    public LoadedDiceSideSelectedMessage(int? selectedSide)
    {
        SelectedSide = selectedSide;
    }
}

[Serializable, NetSerializable]
public enum LoadedDiceUiKey : byte
{
    Key
}
