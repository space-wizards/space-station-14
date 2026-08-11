using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for a <see cref="BorgModuleComponent"/> that provides actions to the entity it's installed into.
/// </summary>
/// <remarks>
/// The provided actions are removed when the module is uninstalled.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBorgSystem))]
public sealed partial class ActionBorgModuleComponent : Component
{
    /// <summary>
    /// What actions should be granted once this module is installed into a borg chassis.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId<ActionComponent>> Actions = new();

    [DataField]
    public List<EntityUid> ActionUids = new();
}
