using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Proxy-component that shows stamina damage from the <see cref="StaminaDamageOnCollideComponent"/>, <see cref="StaminaDamageOnHitComponent"/>, and <see cref="StaminaDamageOnEmbedComponent"/>.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedStaminaSystem))]
public sealed partial class StaminaDamageExaminableComponent : Component
{
    /// <summary>
    /// LocId for message that will be shown on detailed examine.
    /// </summary>
    [DataField]
    public LocId ExamineMessage = "stamina-damage-examine-start";

    /// <summary>
    /// LocId for verb name.
    /// </summary>
    [DataField]
    public LocId VerbName = "stamina-damage-examine-verb";

    /// <summary>
    /// LocId for verb name.
    /// </summary>
    [DataField]
    public LocId VerbMsg = "stamina-damage-examine-text";
}
