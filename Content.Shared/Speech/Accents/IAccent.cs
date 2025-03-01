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
    /// <param name="randomSeed">Seed used determine randomness. Usually the message uuid.</param>
    public string Accentuate(string message, int randomSeed);

}
