using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Guardian.Components;

/// <summary>
/// Creates a GuardianComponent attached to the user's GuardianHost.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GuardianCreatorComponent : Component
{
    /// <summary>
    /// Counts as spent upon exhausting the injection
    /// </summary>
    /// <remarks>
    /// We don't mark as deleted as examine depends on this.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool Used;

    /// <summary>
    /// Whether the creator is an injector that injects the guardian into a host.
    /// </summary>
    /// <remarks>
    /// For methods done through injection.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool Injector;

    /// <summary>
    /// Popup shown when the injector has already been used and cannot create a guardian.
    /// </summary>
    [DataField]
    public LocId InjectorEmptyPopup = "guardian-injector-empty-invalid-creation";

    /// <summary>
    /// Examine text shown for an exhausted injector creator.
    /// </summary>
    [DataField]
    public LocId InjectorEmptyExamine = "guardian-injector-empty-examine";

    /// <summary>
    /// Whether the creator is a deck that grants a guardian to its user.
    /// </summary>
    /// <remarks>
    /// For methods obtained through a deck.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool Deck;

    /// <summary>
    /// Popup shown when the deck can no longer produce a guardian.
    /// </summary>
    [DataField]
    public LocId DeckUsedPopup = "guardian-deck-invalid-creation";

    /// <summary>
    /// Examine text shown for a guardian deck that has already been used.
    /// </summary>
    [DataField]
    public LocId DeckUsedExamine = "guardian-deck-used-examine";

    /// <summary>
    /// Text shown to the host indicating the guardian creation was successful.
    /// </summary>
    [DataField]
    public LocId GuardianHauntedPopup = "guardian-created";

    /// <summary>
    /// The prototype of the guardian entity which will be created
    /// </summary>
    [DataField(required: true)]
    public EntProtoId? GuardianProto { get; set; }

    /// <summary>
    /// How long it takes to inject someone.
    /// </summary>
    [DataField("delay")]
    public float InjectionDelay = 5f;
}
