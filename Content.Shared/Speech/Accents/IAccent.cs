using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public interface IAccent
{
    /// <summary>
    /// The name used to match accent tags with a certain accent.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Accentuate text, returning it with the accent.
    /// </summary>
    /// <param name="message">The input string.</param>
    /// <param name="attributes">Optional attributes that may have been included in the node.</param>
    /// <param name="randomSeed">Seed used determine randomness. Usually the message uuid.</param>
    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed);

    /// <summary>
    /// Used to get the accent name for markup purposes, as well as other data.
    /// Should at the very least add the accent name via ev.Add(Name);
    /// </summary>
    /// <param name="ev">The event which retrieves the accent data.</param>
    /// <param name="c">The component from which the event is ran.</param>
    public void GetAccentData(ref AccentGetEvent ev, Component c);
}
