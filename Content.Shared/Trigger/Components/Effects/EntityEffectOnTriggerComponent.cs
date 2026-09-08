using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Applies a list of entity effects to the owning entity when triggered.
/// If TargetUser is true then they will be applied to the user instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The effects to apply.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityEffect[] Effects;

    /// <summary>
    /// Optional scale multiplier for the effects.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Scale = 1f;
}
