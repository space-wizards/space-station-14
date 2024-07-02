using Robust.Shared.GameStates;

namespace Content.Server.Access.Components;
/// <summary>
/// Makes it so when starting gear loads up, the name on a PDA/Id (if present) is changed to the character's name.
/// </summary>

[RegisterComponent]
public sealed partial class IdBindComponent : Component
{
    /// <summary>
    /// If true, also tries to get the PDA and set the owner to the entity
    /// </summary>
    [DataField]
    public bool BindPDAOwner = true;

}

