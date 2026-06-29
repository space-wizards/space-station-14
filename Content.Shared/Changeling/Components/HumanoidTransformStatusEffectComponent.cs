using Content.Shared.Cloning;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// This component is used for status effects that force an entity to transform into another identity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HumanoidTransformStatusEffectComponent : Component
{
    /// <summary>
    /// The identity to transform the target into when the status effect is applied.
    /// Must be set by the system adding the status effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TargetIdentity;

    /// <summary>
    /// The original identity to revert the target into when the status effect is removed.
    /// This is automatically populated when the status effect is applied if not already set.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OriginalIdentity;

    /// <summary>
    /// The cloning settings to use for the transformation and reversion.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> CloningSettings = "BaseClone";
}
