using Content.Server.Objectives.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Grants the listed objectives to any mind that enters this entity.
/// Useful for ghost role bodies that should carry objectives without being
/// assigned by a game rule (e.g. admin-spawned adventurers).
/// </summary>
[RegisterComponent, Access(typeof(AutoObjectivesSystem))]
public sealed partial class AutoObjectivesComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Objectives = new();
}
