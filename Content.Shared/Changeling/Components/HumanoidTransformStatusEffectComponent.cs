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
    /// The original identity to revert the target to when the status effect is removed.
    /// This is populated when the status effect is applied if not already set.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OriginalIdentity;

    /// <summary>
    /// The cloning settings to use for the transformation and reversion.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> CloningSettings = "BaseClone";
}
