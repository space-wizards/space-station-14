using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when a user opens a UI belonging to the owning entity.
/// The user is the actor that tries to open a UI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUiOpenComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// If it should only work on specific UIs.
    /// Null means it will work on any UI key.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<Enum>? UiKeys;
}
