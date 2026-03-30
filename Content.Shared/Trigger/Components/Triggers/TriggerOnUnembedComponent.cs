using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when this entity gets un-embedded from something.
/// User is the item that was embedded or the actual embed depending on <see cref="UserIsEmbed"/>
/// Handled by <seealso cref="TriggerOnEmbedSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUnembedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// If false the trigger user will be the one that detaches the embedded entity.
    /// If true, the trigger user will be the entity the projectile was embedded into.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UserIsEmbeddedInto;
}
