using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// Spawns a linked group of ghost roles that enter the world together.
/// On spawn it places one ghost role marker per member at its position. Players
/// who claim a role wait in a dialog until every slot is claimed, then the whole
/// party spawns simultaneously. See <see cref="GhostRolePartySystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(GhostRolePartySystem))]
public sealed partial class GhostRolePartyControllerComponent : Component
{
    /// <summary>
    /// Ghost role marker prototypes to place, one per party member.
    /// Each must have a <see cref="GhostRolePartySpawnerComponent"/>.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Members = new();

    /// <summary>
    /// Runtime state of each party slot.
    /// </summary>
    [ViewVariables]
    public List<GhostRolePartySlot> Slots = new();

    /// <summary>
    /// Set once all slots are claimed and the party is being spawned;
    /// cancels arriving after this point are ignored.
    /// </summary>
    [ViewVariables]
    public bool Spawning;
}

/// <summary>
/// One member slot of a ghost role party.
/// </summary>
public sealed class GhostRolePartySlot
{
    /// <summary>The marker prototype for this slot, respawned when a claim is cancelled.</summary>
    public EntProtoId SpawnerProto;

    /// <summary>Humanoid settings spawned for this member when the party is ready.</summary>
    public ProtoId<RandomHumanoidSettingsPrototype> Settings;

    /// <summary>Mind roles added to the member's mind on spawn.</summary>
    public List<EntProtoId> MindRoles = new();

    /// <summary>
    /// Name given to the spawned mob if the humanoid settings don't rename it.
    /// Taken from the marker's ghost role name.
    /// </summary>
    public string FallbackName = string.Empty;

    /// <summary>Where the marker sits and the member will spawn.</summary>
    public EntityCoordinates Coordinates;

    /// <summary>The current unclaimed marker entity, if any.</summary>
    public EntityUid? Spawner;

    /// <summary>The player who has claimed this slot, if any.</summary>
    public ICommonSession? Session;

    /// <summary>The waiting dialog shown to the claiming player.</summary>
    public GhostRolePartyWaitingEui? Eui;

    public GhostRolePartySlot(EntProtoId spawnerProto, EntityCoordinates coordinates)
    {
        SpawnerProto = spawnerProto;
        Coordinates = coordinates;
    }
}
