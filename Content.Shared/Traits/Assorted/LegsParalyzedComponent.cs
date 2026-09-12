using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Leg Paralysis, Should be paired with spawning and buckling of a wheelchair round start
///
/// TODO: This is in leu of New medical. A better way to accomplish this is to remove a persons legs/leg function on roundstart
///     as part of the trait. When a new medical releases in any form, do that instead and annihilate this.
/// </summary>
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent, Access(typeof(LegsParalyzedSystem))]
public sealed partial class LegsParalyzedComponent : Component
{

    /// <summary>
    /// Set the players new Base walk speed, Should be low because their legs are non-functional in some manner and they need to move by other means.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BaseWalkSpeed = 1;

    ///<summary>
    /// Set the players new Base speed, Should be low because their legs are non-functional in some manner and they need to move by other means.
    ///</summary>
    [DataField, AutoNetworkedField]
    public int BaseSprintSpeed = 1;

    ///<summary>
    ///Does the player drop their items when unbuckled.
    ///</summary>
    [DataField, AutoNetworkedField]
    public bool DropOnUnbuckle;
}
