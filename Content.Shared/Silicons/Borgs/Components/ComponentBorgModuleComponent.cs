using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for a <see cref="BorgModuleComponent"/> that provides components to the entity it's installed into.
/// </summary>
/// <remarks>
/// The provided components are removed when the module is uninstalled.
/// If a chassis has a FooComponent, a module adds FooComponent as well and then is uninstalled, then chassis will lose FooComponent.
/// </remarks>
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
