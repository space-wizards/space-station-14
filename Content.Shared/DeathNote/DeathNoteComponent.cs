using Robust.Shared.GameStates;

namespace Content.Shared.DeathNote;

/// <summary>
/// Paper with that component is Death Note.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DeathNoteComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> TouchedBy = new();

    [DataField]
    public bool HasRules;
}
