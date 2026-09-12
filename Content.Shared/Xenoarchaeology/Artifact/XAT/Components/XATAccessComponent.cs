using Content.Shared.Access;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// Trigger that activates when an entity interacts with the artifact with the appropriate access
/// AccessReaderComponent must also be attached to the trigger and holds all the datafields, this just facilitates it triggering the artifact
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATAccessSystem)), AutoGenerateComponentState]
public sealed partial class XATAccessComponent : Component
{
    /// <summary>
    /// Sound made on successful access
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? AccessSound;

    /// <summary>
    /// Sound made on unsuccessful access
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeniedSound;

    /// <summary>
    /// List of potential accesses this trigger could have
    /// Added on to any accesses AccessReaderComponent has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>>? PotentialAccess = new();

    [DataField, AutoNetworkedField]
    public LocId ExamineString = "xenoarch-trigger-examine-access";

    /// <summary>
    /// What to show in popup after an interaction with an ID that doesn't have the correct access.
    /// If null - no popup will be shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? WrongAccessPopup = "interact-artifact-wrong-access";
}
