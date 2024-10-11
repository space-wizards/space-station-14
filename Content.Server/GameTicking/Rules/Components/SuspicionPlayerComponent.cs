namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Prevents players from being revived by defibrillators and will also trigger an alert in chat if the dead person is examined.
/// </summary>
[RegisterComponent]
public sealed partial class SuspicionPlayerComponent : Component
{
    /// <summary>
    /// If true, the examine window will show the dead person's role.
    /// </summary>
    public bool Revealed = false;
}
