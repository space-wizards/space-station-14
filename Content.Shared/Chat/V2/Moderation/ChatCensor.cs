using System.Linq;

namespace Content.Shared.Chat.V2.Moderation;

/// <summary>
/// Censors a chat string. Returns true if the chat was censored.
/// </summary>
public delegate bool Censor(string input, out string output, char replaceWith = '*');

public static class ChatCensor
{
    // SINGLETON PATTERN HO!

    public static Censor Censor { get; private set; } = NoOpCensor;
    private static List<Censor> _censors = new();

    public static void With(Censor censor)
    {
        _censors.Add(censor);
    }

    /// <summary>
    /// Builds a ChatCensor that combines all the censors that have been added to this.
    /// </summary>
    public static void Build()
    {
        // Copy over so the factory can be used again without pollution.
        // Closures, yippee.
        var censors = new List<Censor>(_censors);

        Censor = (string input, out string output, char with) =>
        {
            var censored = false;

            foreach (var _ in censors.Where(censor => censor(input, out input, with)))
            {
                censored = true;
            }

            output = input;

            return censored;
        };
    }

    /// <summary>
    /// A censor that does nothing.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="_"></param>
    /// <returns></returns>
    static bool NoOpCensor(string input, out string output, char _ = '*')
    {
        output = input;

        return false;
    }
}
