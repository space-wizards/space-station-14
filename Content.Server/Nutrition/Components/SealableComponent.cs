using Content.Server.Nutrition.EntitySystems;

namespace Content.Server.Nutrition.Components;

/// <summary>
/// Represents a tamper-evident seal on an Openable.
/// Only affects the Examine text.
/// Once the seal has been broken, it cannot be resealed.
/// </summary>
[RegisterComponent, Access(typeof(SealableSystem))]
public sealed partial class SealableComponent : Component
{
    /// <summary>
    /// Whether the item's seal is intact (i.e. it has never been opened)
    /// </summary>
    [DataField]
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
