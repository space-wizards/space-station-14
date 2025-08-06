namespace Content.Server._Starlight.Language;

// <summary>
//     Signifies that this entity can speak and understand any language.
//     Applies to such entities as ghosts.
// </summary>
[RegisterComponent]
public sealed partial class UniversalLanguageSpeakerComponent : Component
{
    [DataField]
    public bool Enabled = true;
}