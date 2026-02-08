using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Makes this entity speak a certain phrase when melee attacking.
/// Can also be added to melee weapons.
/// The owner can set the phrase using an action.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]

public sealed partial class MeleeSpeechComponent : Component
{
    /// <summary>
    /// The battlecry to be said when an entity attacks with this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Battlecry;

    /// <summary>
    /// The maximum amount of characters allowed in a battlecry.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxBattlecryLength = 12;

    /// <summary>
    /// The action prototype for opening the battlecry UI.
    /// Set to null if the owner should not be able to set their battlecry themselves.
    /// </summary>
    [DataField]
    public EntProtoId? ConfigureAction = "ActionConfigureMeleeSpeech";

    /// <summary>
    /// The action entity for opening the battlecry UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ConfigureActionEntity;
}

/// <summary>
/// BUI key for the battlecry UI.
/// </summary>
[Serializable, NetSerializable]
public enum MeleeSpeechUiKey : byte
{
    Key,
}

/// <summary>
/// Send by the client when trying to change the battlecry.
/// </summary>
[Serializable, NetSerializable]
public sealed class MeleeSpeechBattlecryChangedMessage(string battlecry) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The new battlecry.
    /// </summary>
    public string Battlecry = battlecry;
}

/// <summary>
/// Raised when using the action for opening the battlecry BUI.
/// </summary>
public sealed partial class MeleeSpeechConfigureActionEvent : InstantActionEvent { }
