using Content.Server.Thief.Systems;

namespace Content.Server.Thief.Components;

/// <summary>
/// Makes the entity a thief either instantly if it has a mind or when a mind is added.
/// </summary>
[RegisterComponent, Access(typeof(AutoThiefSystem))]
public sealed partial class AutoThiefComponent : Component
{
    /// <summary>
    /// Whether to give the pacified component
    /// </summary>
    [DataField]
    public bool AddPacified = true;
}
