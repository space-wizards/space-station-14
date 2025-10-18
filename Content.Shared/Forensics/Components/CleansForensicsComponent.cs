using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component is for items that can clean up forensic evidence
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CleansForensicsComponent : Component
{
    /// <summary>
    /// How long it takes to wipe prints/blood/etc. off of things using this entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CleanDelay = TimeSpan.FromSeconds(12.0f);
}
