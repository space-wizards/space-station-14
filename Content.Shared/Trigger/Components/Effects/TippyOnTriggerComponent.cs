using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Sends a tippy message to either the entity or all players when triggered.
/// If TargetUser is true the user will receive the message.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TippyOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Unlocalized message text to send to the player(s).
    /// Intended only for admeme purposes. For anything else you should use <see cref="LocMessage"/> instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Message = string.Empty;

    /// <summary>
    /// Localized message text to send to the player(s).
    /// This has priority over <see cref="Message"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? LocMessage;

    /// <summary>
    /// If true the message will be send to all players.
    /// If false it will be send to the user or owning entity, depending on <see cref="BaseXOnTriggerComponent.TargetUser"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SendToAll;

    /// <summary>
    /// The entity prototype to show to the client.
    /// Will default to tippy if null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Prototype;

    /// <summary>
    /// Use the prototype of the entity owning this component?
    /// Will take priority over <see cref="Prototype"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseOwnerPrototype;

    /// <summary>
    /// The time the speech bubble is shown, in seconds.
    /// Will be calculated automatically from the message length if null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? SpeakTime;

    /// <summary>
    /// The time the entity takes to walk onto the screen, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SlideTime = 3f;

    /// <summary>
    /// The time between waddle animation steps, in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WaddleInterval = 0.5f;
}
