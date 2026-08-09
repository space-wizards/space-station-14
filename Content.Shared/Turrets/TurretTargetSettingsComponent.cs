using Content.Shared.Access;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Turrets;

/// <summary>
/// Attached to entities to provide them with turret target selection data.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TurretTargetSettingsSystem))]
public sealed partial class TurretTargetSettingsComponent : Component
{
    /// <summary>
    /// Crew with one or more access levels from this list are exempt from being targeted by turrets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> ExemptAccessLevels = new();

    // DS14-start
    /// <summary>
    /// Factions used to determine whether another entity is friendly to the turret.
    /// Falls back to the turret's NPC factions when empty.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<NpcFactionPrototype>> TurretFactions = new();
    // DS14-end
}
