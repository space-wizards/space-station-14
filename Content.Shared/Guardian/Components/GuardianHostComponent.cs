using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.Guardian.Components;

/// <summary>
/// Given to guardian users upon establishing a guardian link with the entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GuardianHostComponent : Component
{
    /// <summary>
    /// Guardian hosted within the component.
    /// </summary>
    /// <remarks>
    /// Can be null if the component is added at any time.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public EntityUid? HostedGuardian;

    /// <summary>
    /// Container which holds the guardian
    /// </summary>
    [ViewVariables]
    public ContainerSlot GuardianContainer;

    /// <summary>
    /// Action prototype used to toggle (release/retract) the hosted guardian.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionToggleGuardian";

    /// <summary>
    /// Popup shown to the guardian when its host enters a critical state.
    /// </summary>
    [DataField]
    public LocId GuardianHostCritWarn = "guardian-host-critical-warn";

    /// <summary>
    /// Popup shown to the host when its guardian is retracted.
    /// </summary>
    [DataField]
    public LocId GuardianHostRecall = "guardian-entity-recall";

    /// <summary>
    /// The spawned action entity used to toggle the guardian.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Sound played when the guardian is released.
    /// </summary>
    [DataField]
    public SoundSpecifier ReleaseSound = new SoundPathSpecifier("/Audio/Effects/guardian_warn.ogg");
}
