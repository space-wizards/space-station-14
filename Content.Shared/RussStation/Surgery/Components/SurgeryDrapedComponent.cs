using Content.Shared.RussStation.Surgery.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.RussStation.Surgery.Components;

/// <summary>
/// Marks an entity as draped with a bedsheet and ready for surgery.
/// The bedsheet entity is stored and dropped when surgery ends.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryDrapedComponent : Component
{
    /// <summary>
    /// The bedsheet entity that was used to drape this patient.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Bedsheet;
}
