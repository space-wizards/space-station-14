namespace Content.Shared.Magic;

// TODO: Move to magic component
// TODO: See if you can turn into an event or something so spells don't need to be in server
public interface ISpeakSpell // The speak n spell interface
{
    /// <summary>
    /// Localized string spoken by the caster when casting this spell.
    /// </summary>
    public string? Speech { get; }
}
