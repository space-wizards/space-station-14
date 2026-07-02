using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component is for items that can clean up forensic evidence
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CleansForensicsComponent : Component
{
    /// <summary>
    /// How long it takes to wipe prints/blood/etc. off of things using this entity
    /// </summary>
    [DataField]
    public float CleanDelay = 12.0f;
}
