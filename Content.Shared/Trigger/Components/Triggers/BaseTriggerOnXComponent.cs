using Content.Shared.Trigger.Systems;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Base class for components that cause a trigger to be activated.
/// </summary>
public abstract partial class BaseTriggerOnXComponent : Component
{
    /// <summary>
    /// The key that the trigger will activate.
    /// null will activate all triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? KeyOut = TriggerSystem.DefaultTriggerKey;
}
