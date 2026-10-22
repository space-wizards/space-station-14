using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// Marks entity as bible user.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BibleSystem))]
public sealed partial class BibleUserComponent : Component
{
    public override bool SendOnlyToOwner => true;
}
