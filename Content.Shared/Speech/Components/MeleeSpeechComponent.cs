using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MeleeSpeechComponent : Component
{
    /// <summary>
    /// The battlecry to be said when an entity attacks with this component
    /// </summary>
    [DataField("Battlecry")]
    [AutoNetworkedField]
    public string? Battlecry;

    /// <summary>
    /// The maximum amount of characters allowed in a battlecry
    /// </summary>
    [DataField("MaxBattlecryLength")]
    [AutoNetworkedField]
    public int MaxBattlecryLength = 12;

    [DataField]
    public EntProtoId ConfigureAction = "ActionConfigureMeleeSpeech";

    /// <summary>
    /// The action to open the battlecry UI
    /// </summary>
    [DataField]
    public EntityUid? ConfigureActionEntity;
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
public sealed class MeleeSpeechBoundUserInterfaceState(string currentBattlecry) : BoundUserInterfaceState
{
    public string CurrentBattlecry { get; } = currentBattlecry;
}

[Serializable, NetSerializable]
public sealed class MeleeSpeechBattlecryChangedMessage(string battlecry) : BoundUserInterfaceMessage
{
    public string Battlecry { get; } = battlecry;
}

public sealed partial class MeleeSpeechConfigureActionEvent : InstantActionEvent;
