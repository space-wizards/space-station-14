using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BibleUserComponent : Component {
    public override bool SendOnlyToOwner => true;
}
