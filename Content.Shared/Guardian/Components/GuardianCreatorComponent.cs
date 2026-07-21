using Robust.Shared.Audio;
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
    /// Sound played when a mob obtains a guardian.
    /// </summary>
    [DataField]
    public SoundSpecifier UsedSound = new SoundPathSpecifier("/Audio/Effects/guardian_inject.ogg");

    /// <summary>
    /// Popup shown when the creator has already been used and cannot create a guardian.
    /// </summary>
    [DataField]
    public LocId EmptyPopup = "guardian-injector-empty-invalid-creation";

    /// <summary>
    /// Examine text shown for an exhausted creator.
    /// </summary>
    [DataField]
    public LocId EmptyExamine = "guardian-injector-empty-examine";

    /// <summary>
    /// Text shown to the host indicating the guardian creation was successful.
    /// </summary>
    [DataField]
    public LocId GuardianHauntedPopup = "guardian-created";

    /// <summary>
    /// The prototypes of the guardian entities which will be created
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<GuardianEntryPrototype>> Guardians { get; set; }

    /// <summary>
    /// If true, then the user will get a radial UI to choose its guardian (from Guardians).
    /// If false, the guardian will be randomly picked.
    /// </summary>
    [DataField]
    public bool CanChoose = true;

    /// <summary>
    /// Current selected guardian type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint Selected = 0;

    /// <summary>
    /// How long it takes to inject someone.
    /// </summary>
    [DataField("delay")]
    public float InjectionDelay = 5f;
}
