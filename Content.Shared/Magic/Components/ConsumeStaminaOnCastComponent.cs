using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Magic.Components;

/// <summary>
/// Adds a stamina cost to an action/spell. Handled during <see cref="Events.BeforeCastSpellEvent"/>
/// so that casting can be cancelled when the performer lacks stamina.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMagicSystem))]
public sealed partial class ConsumeStaminaOnCastComponent : Component
{
    /// <summary>
    /// Amount of stamina to consume when casting this action.
    /// </summary>
    [DataField]
    public float Amount = 0f;
}
