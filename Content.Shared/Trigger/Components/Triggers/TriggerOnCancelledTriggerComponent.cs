using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Translates a cancelled trigger into a new trigger.
/// The user of the new trigger is the same as the cancelled trigger.
/// </summary>
/// <remarks> WARNING: Has the potential to cause an infinite loop by listening for its own trigger. </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnCancelledTriggerComponent : Component
{
    /// <summary>
    /// KeyIn followed by KeyOut. Does not support a KeyIn being identical to KeyOut.
    /// KeyIn: The key that will trigger the KeyOut.
    /// KeyOut: The trigger key that will activate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> KeysInOut = new(){ [TriggerSystem.DefaultTriggerKey] = TriggerSystem.CancelledTriggerKey };
}
