using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Configuration for the transformation sting action.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingTransformationStingAbilityComponent : Component
{
    /// <summary>
    /// How long the forced transformation should last.
    /// </summary>
    [DataField]
    public TimeSpan TransformationDuration = TimeSpan.FromSeconds(120f);

    /// <summary>
    /// The transformation status effect to apply.
    /// </summary>
    [DataField]
    public EntProtoId TransformationStatusEffect = "StatusEffectHumanoidTransform";
}

/// <summary>
/// Action event for the transformation sting ability.
/// </summary>
public sealed partial class ChangelingTransformationStingEvent : EntityTargetActionEvent;
