using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Sends IC chat messages whenever doing melee combat.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MeleeSpeechComponent : Component
{
    /// <summary>
    /// The text to send.
    /// </summary>
	[ViewVariables(VVAccess.ReadWrite)]
	[DataField("Battlecry")]
	[AutoNetworkedField]
	public string? Battlecry;

    /// <summary>
    /// Max text length.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("MaxBattlecryLength"), AutoNetworkedField]
    public int MaxBattlecryLength = 12;
}

/// <summary>
/// Key representing which <see cref="BoundUserInterface"/> is currently open.
/// Useful when there are multiple UI for an object. Here it's future-proofing only.
/// </summary>
[Serializable, NetSerializable]
public enum MeleeSpeechUiKey : byte
{
    Key,
}

/// <summary>
/// Represents an <see cref="MeleeSpeechComponent"/> state that can be sent to the client
/// </summary>
[Serializable, NetSerializable]
public sealed class MeleeSpeechBoundUserInterfaceState : BoundUserInterfaceState
{
    public string CurrentBattlecry;

    public MeleeSpeechBoundUserInterfaceState(string currentBattlecry)
    {
        CurrentBattlecry = currentBattlecry;
    }
}

[Serializable, NetSerializable]
public sealed class MeleeSpeechBattlecryChangedMessage : BoundUserInterfaceMessage
{
    public string Battlecry;

    public MeleeSpeechBattlecryChangedMessage(string battlecry)
    {
        Battlecry = battlecry;
    }
}
