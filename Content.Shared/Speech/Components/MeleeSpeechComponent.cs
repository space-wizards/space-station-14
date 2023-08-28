using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]

public sealed partial class MeleeSpeechComponent : Component
{
    /// <summary>
    /// The battlecry to be said when an entity attacks with this component
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("Battlecry")]
    [AutoNetworkedField]
    public string? Battlecry;

    /// <summary>
    /// The maximum amount of characters allowed in a battlecry
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("MaxBattlecryLength")]
    [AutoNetworkedField]
    public int MaxBattlecryLength = 12;

    /// <summary>
    /// The action to open the battlecry UI
    /// </summary>
    [DataField("configureAction")]
    public InstantAction ConfigureAction = new()
    {
        UseDelay = TimeSpan.FromSeconds(1),
        ItemIconStyle = ItemActionIconStyle.BigItem,
        DisplayName = "melee-speech-config",
        Description = "melee-speech-config-desc",
        Priority = -20,
        Event = new MeleeSpeechConfigureActionEvent(),
    };
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
    public string CurrentBattlecry { get; }
    public MeleeSpeechBoundUserInterfaceState(string currentBattlecry)
    {
        CurrentBattlecry = currentBattlecry;
    }
}

[Serializable, NetSerializable]
public sealed class MeleeSpeechBattlecryChangedMessage : BoundUserInterfaceMessage
{
    public string Battlecry { get; }
    public MeleeSpeechBattlecryChangedMessage(string battlecry)
    {
        Battlecry = battlecry;
    }
}

public sealed partial class MeleeSpeechConfigureActionEvent : InstantActionEvent { }
