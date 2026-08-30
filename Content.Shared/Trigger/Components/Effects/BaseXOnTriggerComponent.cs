using Content.Shared.Trigger.Systems;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Base class for components that do something when triggered.
/// </summary>
public abstract partial class BaseXOnTriggerComponent : Component
{
    /// <summary>
    /// The keys that will activate the effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> KeysIn = new() { TriggerSystem.DefaultTriggerKey };

    /// <summary>
    /// Set to true to make the user of the trigger the effect target.
    /// Set to false to make the owner of this component the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetUser = false;
}
