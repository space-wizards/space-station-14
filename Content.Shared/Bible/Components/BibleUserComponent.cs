using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// For humanoids/entities that can interact with bibles, religion related things. i.e., summon entity with bible or pray on bible.
/// </summary>
/// <remarks>
/// Only chapelain's client get networked and informed as bibleUser. No other entity get informed.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class BibleUserComponent : Component
{
    public override bool SendOnlyToOwner => true;
}
