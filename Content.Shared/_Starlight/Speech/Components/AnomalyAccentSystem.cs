using System.Linq;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Shared._Starlight.Speech.Components;

public sealed class AnomalyAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, AnomalyAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        var words = message.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = ApplyAnomalyToWord(words[i]);
            words[i] = ApplyCaseChange(words[i]);
        }

        args.Message = string.Join(" ", words);
    }

    private string ApplyAnomalyToWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return word;

        var chars = word.ToCharArray();
        var length = chars.Length;
        var replaceCount = _random.Next(Math.Max(1, length / 5), Math.Max(1, (length * 2) / 5) + 1); // with 100% chance (20-40% of word length) changes to #$@#$!
        var indices = Enumerable.Range(0, length).OrderBy(_ => _random.Next()).Take(replaceCount).ToList();

        foreach (var index in indices)
        {
            chars[index] = _random.Pick(new[] { '#', '@', '!', '*', '%', '$', '^', '&', '~', '?' });
        }

        return new string(chars);
    }

    private string ApplyCaseChange(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || !_random.Prob(0.4f)) // with 40% chance 1-3 symbols at word will change case
            return word;

        var chars = word.ToCharArray();
        var length = chars.Length;
        var changeCount = _random.Next(1, Math.Min(4, length + 1));
        var indices = Enumerable.Range(0, length).OrderBy(_ => _random.Next()).Take(changeCount).ToList();

        foreach (var index in indices)
        {
            if (char.IsLetter(chars[index]))
            {
                chars[index] = char.IsUpper(chars[index]) ? char.ToLower(chars[index]) : char.ToUpper(chars[index]);
            }
        }

        return new string(chars);
    }
}
