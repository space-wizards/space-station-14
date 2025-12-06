using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Access;

namespace Content.Shared.Mech.Components;

/// <summary>
/// Types of mech locks
/// </summary>
public enum MechLockType
{
    Dna,
    Card
}

/// <summary>
/// Component for managing mech lock system (DNA and Card locks)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MechLockComponent : Component
{
    /// <summary>
    /// Whether DNA lock is registered
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DnaLockRegistered = false;

    /// <summary>
    /// Whether DNA lock is active (prevents access)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DnaLockActive = false;

    /// <summary>
    /// DNA of the lock owner
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? OwnerDna;

    /// <summary>
    /// Whether ID card lock is registered
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CardLockRegistered = false;

    /// <summary>
    /// Whether ID card lock is active (prevents access)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CardLockActive = false;

    /// <summary>
    /// Localized job title of the lock owner (for UI display)
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? OwnerJobTitle;

    /// <summary>
    /// Access tags captured from the registered ID card
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>>? CardAccessTags;

    /// <summary>
    /// Whether the mech is locked (prevents unauthorized access)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsLocked = false;
}
