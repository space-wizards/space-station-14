
using Content.Shared.Damage;
using Content.Shared.Repairable;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Repairable;

/// <summary>
/// Heals damage to an entity by using up another entity.
/// <see cref="Repairable.RepairableComponent"/> for healing via tool.
/// </summary>
[RegisterComponent]
public sealed partial class RepairableByReplacementComponent : Component
{
    /// <summary>
    /// An entity with this tag can repair this entity
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<TagPrototype>, RepairMaterialSpecifier> RepairTypes = new();

}
