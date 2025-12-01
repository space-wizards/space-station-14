using Content.Shared.Resist.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Resist.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ResistLockerSystem))]
public sealed partial class ResistLockerComponent : Component
{
    /// <summary>
    /// How long will this locker take to kick open, defaults to 2 minutes
    /// </summary>
    [DataField]
    public float ResistTime = 120f;

    /// <summary>
    /// For quick exit if the player attempts to move while already resisting
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsResisting = false;
}
