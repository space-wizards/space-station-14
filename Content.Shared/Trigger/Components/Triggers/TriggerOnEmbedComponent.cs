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
    /// Whether the trigger user will be the one that embedded them.
    /// If true, the trigger user will be the actual embed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UserIsEmbed;
}
