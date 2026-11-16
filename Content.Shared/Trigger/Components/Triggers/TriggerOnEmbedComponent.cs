using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when this entity first embeds into something.
/// User is the item that was embedded or the actual embed depending on <see cref="UserIsEmbed"/>
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
