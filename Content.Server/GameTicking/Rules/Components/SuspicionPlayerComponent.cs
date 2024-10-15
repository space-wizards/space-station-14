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
    [ViewVariables]
    public bool Revealed = false;

    /// <summary>
    /// How many units (probably meters? idk what this game uses) a person must be from the station to be considered "off-station" and start taking damage.
    /// </summary>
    [ViewVariables]
    public float SpacewalkThreshold = 15;

    [ViewVariables]
    public DateTime LastTookSpacewalkDamage = DateTime.Now;
}
