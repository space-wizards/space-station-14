using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers on an item embedding into something.
/// User is the item that was embedded or the actual embed depending on <see cref="UserIsEmbed"/>
/// Handled by <seealso cref="TriggerOnEmbedSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUnembedComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Whether the trigger user will be the one that detached them.
    /// If true, the trigger user will be the actual embed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UserIsEmbed;
}
