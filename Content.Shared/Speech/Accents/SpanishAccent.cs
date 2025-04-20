using System.Text;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class SpanishAccent : IAccent
{
    public string Name { get; } = "Spanish";

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        // Insert E before every S
        message = InsertS(message);
        // If a sentence ends with ?, insert a reverse ? at the beginning of the sentence
        message = ReplacePunctuation(message);
        return message;
    }

    private string InsertS(string message)
    {
        // Replace every new Word that starts with s/S
        var msg = message.Replace(" s", " es").Replace(" S", " Es");

        // Still need to check if the beginning of the message starts
        if (msg.StartsWith("s", StringComparison.Ordinal))
        {
            return msg.Remove(0, 1).Insert(0, "es");
        }
        else if (msg.StartsWith("S", StringComparison.Ordinal))
        {
            return msg.Remove(0, 1).Insert(0, "Es");
        }

        return msg;
    }

    private string ReplacePunctuation(string message)
    {
        var sentences = SharedAccentSystem.SentenceRegex.Split(message);
        var msg = new StringBuilder();
        foreach (var s in sentences)
        {
            var toInsert = new StringBuilder();
            for (var i = s.Length - 1; i >= 0 && "?!‽".Contains(s[i]); i--)
            {
                toInsert.Append(s[i] switch
                {
                    '?' => '¿',
                    '!' => '¡',
                    '‽' => '⸘',
                    _ => ' '
                });
            }
            if (toInsert.Length == 0)
            {
                msg.Append(s);
            } else
            {
                msg.Append(s.Insert(s.Length - s.TrimStart().Length, toInsert.ToString()));
            }
        }
        return msg.ToString();
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}
