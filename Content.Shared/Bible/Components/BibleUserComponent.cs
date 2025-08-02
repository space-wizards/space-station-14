using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// For humanoids/entities that can interact with bibles, religion related things. i.e., summon entity with bible or pray on bible.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BibleUserComponent : Component;
