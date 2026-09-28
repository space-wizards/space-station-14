using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Applies a set of permanent status effects while this component exists.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PermanentStatusEffectsComponent : Component
{
    /// <summary>
    /// The status effects to apply.
    /// </summary>
    [DataField(required: true)]
    public HashSet<EntProtoId> StatusEffects = [];
}
