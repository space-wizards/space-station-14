using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Represents a tamper-evident seal on an Openable.
/// Only affects the Examine text.
/// Once the seal has been broken, it cannot be resealed.
/// </summary>
[NetworkedComponent, AutoGenerateComponentState]
[RegisterComponent, Access(typeof(SealableSystem))]
public sealed partial class SealableComponent : Component
{
    /// <summary>
    /// Whether the item's seal is intact (i.e. it has never been opened)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Sealed = true;

    /// <summary>
    /// Text shown when examining and the item's seal has not been broken.
    /// </summary>
    [DataField]
    public LocId ExamineTextSealed = "drink-component-on-examine-is-sealed";

    /// <summary>
    /// Text shown when examining and the item's seal has been broken.
    /// </summary>
    [DataField]
    public LocId ExamineTextUnsealed = "drink-component-on-examine-is-unsealed";
}
