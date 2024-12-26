using Robust.Shared.GameStates;

namespace Content.Shared.Magic.Components;

// TODO: Rename to MagicActionComponent or MagicRequirementsComponent
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMagicSystem))]
public sealed partial class MagicComponent : Component
{
    // TODO: Split into different components?
    // This could be the MagicRequirementsComp - which just is requirements for the spell
    // Magic comp could be on the actual entities itself
    //  Could handle lifetime, ignore caster, etc?
    // Magic caster comp would be on the caster, used for what I'm not sure

    // TODO: Do After here or in actions

    // TODO: Spell requirements
    //  A list of requirements to cast the spell
    //    Hands
    //    Any item in hand
    //    Spell takes up an inhand slot
    //      May be an action toggle or something

    // TODO: List requirements in action desc
    /// <summary>
    ///     Does this spell require Wizard Robes & Hat?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresClothes;

    /// <summary>
    ///     Does this spell require the user to speak?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresSpeech;

    // TODO: FreeHand - should check if toggleable action
    //  Check which hand is free to toggle action in
}
