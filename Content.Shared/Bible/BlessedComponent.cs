namespace Content.Shared.Bible;

[RegisterComponent]
public sealed partial class BlessedComponent : Component
{
    [DataField]
    public TimeSpan EndTime;
}
