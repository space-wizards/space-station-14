using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers on an item embedding into something.
/// User is the item that was embedded.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnEmbedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// If false the trigger user will be the mob that shot the embeddable projectile.
    /// If true, the trigger user will be the entity the projectile was embedded into.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UserIsEmbeddedInto;
}
