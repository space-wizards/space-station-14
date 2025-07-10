namespace Content.Shared.Bible.Components;

[RegisterComponent]
public sealed partial class BibleUserComponent : Component {
    public override bool SendOnlyToOwner => true;
}
