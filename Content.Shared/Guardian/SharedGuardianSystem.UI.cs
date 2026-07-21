using Robust.Shared.Serialization;

namespace Content.Shared.Guardian;

/// <summary>
///
/// </summary>
/// <param name="targetIdentity"></param>
[Serializable, NetSerializable]
public sealed class GuardianPickedMessage(uint chosenGuardian) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The index of the picked guardian type
    /// </summary>
    public readonly uint ChosenGuardian = chosenGuardian;
}


[NetSerializable, Serializable]
public enum GuardianPickerUiKey : byte
{
    Key
}

