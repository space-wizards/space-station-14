using System.Collections.Generic;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Marks an entity to inflict configured damage when interacted with using a qualifying tool.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnToolInteractComponent : Component
{
    /// <summary>
    /// Damage to apply per matching tool quality.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ToolQualityPrototype>, DamageSpecifier> Damage = new();
}
