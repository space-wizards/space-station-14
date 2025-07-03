using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///     Applies to items that are capable of butchering entities, or
///     are otherwise sharp for some purpose.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SharpComponent : Component
{
    // TODO just make this a tool type.
    [AutoNetworkedField]
    public HashSet<EntityUid> Butchering = [];

    [DataField, AutoNetworkedField]
    public float ButcherDelayModifier = 1.0f;
}
