namespace Content.Shared.ImportantDocument;

/// <summary>
///     Ensures the StealTargetComponent is on the object and then will set the steal target to a random one in
///     the given list.
/// </summary>
[RegisterComponent]
public sealed partial class RandomStealTargetComponent : Component
{
    [DataField(required: true)]
    public List<string> StealTargetNames = new List<string>();
}
