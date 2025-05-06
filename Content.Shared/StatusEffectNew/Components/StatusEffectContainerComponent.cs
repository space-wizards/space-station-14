using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// A component that is automatically added to any entities that have a status effect applied to them. Allows you to track which status effects are applied to an entity right now.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedStatusEffectNewSystem))]
public sealed partial class StatusEffectContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ActiveStatusEffects = new();
}
