using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// A ghost role marker that is one slot of a ghost role party.
/// Claiming the ghost role reserves the slot on the linked controller instead of
/// spawning immediately; the member only spawns once the whole party is ready.
/// </summary>
[RegisterComponent, Access(typeof(GhostRolePartySystem))]
public sealed partial class GhostRolePartySpawnerComponent : Component
{
    /// <summary>
    /// The party controller this spawner belongs to. Set at runtime when the
    /// controller places its markers.
    /// </summary>
    [ViewVariables]
    public EntityUid? Controller;

    /// <summary>
    /// The humanoid settings spawned for this member when the party is ready.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RandomHumanoidSettingsPrototype> Settings;

    /// <summary>
    /// Mind roles added to the member's mind on spawn. Needed because the spawn
    /// bypasses the normal ghost role takeover (which would apply the marker's
    /// GhostRole mind roles).
    /// </summary>
    [DataField]
    public List<EntProtoId> MindRoles = new();
}
