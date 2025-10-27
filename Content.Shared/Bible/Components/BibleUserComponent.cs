using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// For humanoids/entities that can interact with bibles, religion related things. i.e., summon entity with the bible or pray on the bible.
/// </summary>
/// <remarks>
/// Only chaplain's client get networked and informed as <see cref="BibleUserComponent"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class BibleUserComponent : Component
{
    public override bool SendOnlyToOwner => true;
}
