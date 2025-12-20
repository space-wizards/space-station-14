namespace Content.Server.Speech.Components;

[RegisterComponent]
public sealed partial class NegateAccentsComponent : Component
{
    /// <summary>
    /// Should accents be canceled? Defaults to true.
    /// </summary>
    public bool CancelAccent = true;
}
