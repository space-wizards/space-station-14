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
    /// Popup shown when the injector has already been used and cannot create a guardian.
    /// </summary>
    [DataField]
    public LocId EmptyPopup = "guardian-injector-empty-invalid-creation";

    /// <summary>
    /// Examine text shown for an exhausted injector creator.
    /// </summary>
    [DataField]
    public LocId EmptyExamine = "guardian-injector-empty-examine";

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
