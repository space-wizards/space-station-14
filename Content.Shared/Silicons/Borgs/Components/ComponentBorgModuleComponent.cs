using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for a <see cref="BorgModuleComponent"/> that provides components to the entity it's installed into.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBorgSystem))]
public sealed partial class ComponentBorgModuleComponent : Component
{
    /// <summary>
    /// What components should be granted once this module is installed into a borg chassis.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
